// Сборка и тесты FinCtrl с коротким выводом — чтобы консоль не забивала контекст агента.
//   node .claude/scripts/check.mjs backend|frontend|all [--no-tests] [--filter <выражение>]
// --filter: backend — значение для `dotnet test --filter` (например FullyQualifiedName~PaymentService),
//           frontend — шаблон пути/имени spec-файла для vitest (например transferCategory), без vite build.
// Успех — одна строка на часть; провал — до ~40 строк сути (ошибки компиляции, упавшие тесты).
// Полный вывод — в .ai-tmp/check-<часть>.log (папка в .gitignore).
// Exit code: 0 — всё зелёное, 1 — провал, 2 — неверные аргументы.
// Модуль также используется хуком .claude/hooks/pre-pr-check.mjs.
import { spawnSync } from 'node:child_process';
import { existsSync, mkdirSync, readFileSync, writeFileSync } from 'node:fs';
import { dirname, join, resolve } from 'node:path';
import { fileURLToPath, pathToFileURL } from 'node:url';

export const SOLUTION = 'FinCtrl/FinCtrl.Backend.sln';
export const FRONTEND_APP = 'FinCtrl.Frontend/FinCtrl.Frontend';
const LOG_DIR = '.ai-tmp';
const MAX_LINES = 40;

const ANSI = /\x1b\[[0-9;?]*[A-Za-z]/g;
const lines = (text) => text.replace(ANSI, '').split(/\r?\n/);
export const tail = (text, n = MAX_LINES) => text.trim().split(/\r?\n/).slice(-n).join('\n');
const uniq = (arr) => [...new Set(arr)];
const limit = (arr, n = MAX_LINES) => (arr.length > n ? [...arr.slice(0, n), `… ещё ${arr.length - n} строк — см. лог`] : arr);

export function run(root, cmd, args, opts = {}) {
  // Вывод dotnet/VSTest — на английском, чтобы разбор итогов не зависел от локали системы.
  const env = { ...process.env, DOTNET_CLI_UI_LANGUAGE: 'en', VSLANG: '1033', DOTNET_NOLOGO: '1' };
  const spawnOpts = { cwd: root, encoding: 'utf8', maxBuffer: 64 * 1024 * 1024, env };
  // С shell — одной строкой: передача массива аргументов вместе с shell устарела (DEP0190).
  const r = opts.shell
    ? spawnSync([cmd, ...args].join(' '), { ...spawnOpts, shell: true })
    : spawnSync(cmd, args, spawnOpts);
  return { code: r.status ?? 1, out: (r.stdout || '') + (r.stderr || '') };
}

// Строки ошибок MSBuild без хвоста «[путь к проекту]» и абсолютного пути к репозиторию;
// MSBuild повторяет их в конце — убираем дубли.
export function buildErrors(out, root = '') {
  const prefix = root.replace(/\//g, '\\').replace(/\\?$/, '\\').toLowerCase();
  const errors = uniq(lines(out).filter((l) => /: error [A-Z]+\d+/i.test(l))
    .map((l) => l.replace(/\s*\[[^\]]*\]\s*$/, '').trim())
    .map((l) => (prefix.length > 1 && l.toLowerCase().startsWith(prefix) ? l.slice(prefix.length) : l)));
  // Копирование в bin/ заблокировано запущенным процессом — одна понятная строка вместо десятка.
  const locked = errors.filter((l) => /error MSB30(21|27)/.test(l));
  if (locked.length) {
    const who = uniq(locked.map((l) => /"([^"]+\(\d+\))" блокирует|locked by: "([^"]+)"/.exec(l)).filter(Boolean).map((m) => m[1] || m[2]));
    return [`Файлы в bin/ заблокированы запущенным процессом${who.length ? ` (${who.join(', ')})` : ''} — остановите backend (preview_stop или попросите пользователя) и повторите.`,
      ...errors.filter((l) => !locked.includes(l))];
  }
  return errors;
}

// --- backend ---
export function buildBackend(root, { noIncremental = false } = {}) {
  const args = ['build', SOLUTION, '-nologo', '-v', 'q', '-clp:NoSummary'];
  if (noIncremental) args.push('--no-incremental');
  const r = run(root, 'dotnet', args);
  const errors = buildErrors(r.out, root);
  return { ok: r.code === 0, out: r.out, summary: r.code === 0 ? 'build OK' : 'build FAIL', details: errors.length ? errors : lines(tail(r.out)) };
}

export function hasBackendTests(root) {
  return /Tests?\.csproj/.test(readFileSync(join(root, SOLUTION), 'utf8'));
}

export function testBackend(root, filter) {
  const args = ['test', SOLUTION, '--no-build', '-nologo', '-v', 'q', '--logger', 'console;verbosity=minimal'];
  if (filter) args.push('--filter', filter);
  const r = run(root, 'dotnet', args);
  const all = lines(r.out);
  let passed = 0, failed = 0, total = 0;
  for (const l of all) {
    const m = /Failed:\s*(\d+),\s*Passed:\s*(\d+),\s*Skipped:\s*\d+,\s*Total:\s*(\d+)/.exec(l);
    if (m) { failed += +m[1]; passed += +m[2]; total += +m[3]; }
  }
  const counts = total ? `tests ${passed}/${total}` : 'tests';
  if (r.code === 0) return { ok: true, out: r.out, summary: `${counts} OK`, details: [] };
  // Блок упавшего теста: «Failed <имя>», Error Message, Stack Trace — берём имя, сообщение
  // и из стека только первую строку с файлом репозитория (где упало).
  const details = [];
  const repo = root.replace(/\//g, '\\').replace(/\\?$/, '\\').toLowerCase();
  let section = null;
  for (const l of all) {
    const t = l.trim();
    if (/^Failed\s+\S/.test(t) && !/^Failed!/.test(t)) { details.push(t); section = null; continue; }
    if (/^Error Message:/.test(t)) { section = 'message'; continue; }
    if (/^Stack Trace:/.test(t)) { section = 'stack'; continue; }
    if (/^(Standard Output|Passed|Skipped)/.test(t)) { section = null; continue; }
    if (section === 'message' && t) details.push('    ' + t);
    const at = section === 'stack' && / in (.+:line \d+)$/.exec(t);
    if (at && at[1].toLowerCase().startsWith(repo)) { details.push('    at ' + at[1].slice(repo.length)); section = null; }
  }
  const buildErr = buildErrors(r.out, root);
  return { ok: false, out: r.out, summary: `${counts}: FAIL (failed ${failed})`, details: details.length ? details : buildErr.length ? buildErr : lines(tail(r.out)) };
}

// --- frontend ---
const npm = (root, script, extra = []) =>
  run(root, 'npm', ['--prefix', FRONTEND_APP, 'run', script, '--silent', ...(extra.length ? ['--', ...extra.map((a) => JSON.stringify(a))] : [])], { shell: true });

export function buildFrontend(root) {
  const r = npm(root, 'build');
  if (r.code === 0) return { ok: true, out: r.out, summary: 'build OK', details: [] };
  const errs = lines(r.out).filter((l) => /error|Error|ERROR|failed|✘|\[vite\]/.test(l)).map((l) => l.trim()).filter(Boolean);
  return { ok: false, out: r.out, summary: 'build FAIL', details: errs.length ? uniq(errs) : lines(tail(r.out)) };
}

export function hasFrontendTests(root) {
  const pkg = JSON.parse(readFileSync(join(root, FRONTEND_APP, 'package.json'), 'utf8'));
  return Boolean(pkg.scripts?.test);
}

export function testFrontend(root, filter) {
  const r = npm(root, 'test', filter ? [filter] : []);
  const all = lines(r.out);
  const testsLine = all.map((l) => l.trim()).find((l) => /^Tests\s+/.test(l));
  const counts = testsLine ? testsLine.replace(/^Tests\s+/, 'tests ').replace(/\s+/g, ' ') : 'tests';
  if (r.code === 0) return { ok: true, out: r.out, summary: counts, details: [] };
  // FAIL-строки с именем теста и ключевые строки ассертов, без кода и стека.
  const details = uniq(all.map((l) => l.trim()).filter((l) =>
    /^(FAIL|×|✗)\s/.test(l) || /^(AssertionError|TypeError|ReferenceError|Error|SyntaxError):/.test(l) ||
    /^[-+] (Expected|Received)/.test(l) || /^(Expected|Received)/.test(l)));
  return { ok: false, out: r.out, summary: `${counts}: FAIL`, details: details.length ? details : lines(tail(r.out)) };
}

// --- CLI ---
function checkPart(root, part, withTests, filter) {
  const steps = [];
  if (part === 'backend') {
    steps.push(buildBackend(root));
    if (withTests && steps[0].ok && hasBackendTests(root)) steps.push(testBackend(root, filter));
  } else if (filter) {
    // Точечный прогон spec-файлов при итерации: vitest не нужна сборка, vite build пропускаем.
    steps.push(testFrontend(root, filter));
  } else {
    steps.push(buildFrontend(root));
    if (withTests && steps[0].ok && hasFrontendTests(root)) steps.push(testFrontend(root, filter));
  }
  const log = join(root, LOG_DIR, `check-${part}.log`);
  mkdirSync(dirname(log), { recursive: true });
  writeFileSync(log, steps.map((s) => s.out).join('\n\n----\n\n'));
  const ok = steps.every((s) => s.ok);
  const head = `${ok ? 'OK' : 'FAIL'} ${part}: ${steps.map((s) => s.summary).join('; ')}`;
  const failed = steps.find((s) => !s.ok);
  const body = failed ? limit(failed.details).map((l) => '  ' + l).concat(`  полный лог: ${LOG_DIR}/check-${part}.log`) : [];
  return { ok, text: [head, ...body].join('\n') };
}

function main() {
  const args = process.argv.slice(2);
  const fi = args.indexOf('--filter');
  const filter = fi >= 0 ? args[fi + 1] : undefined;
  const target = args.find((a, i) => !a.startsWith('--') && !(fi >= 0 && i === fi + 1));
  const withTests = !args.includes('--no-tests');
  const parts = { backend: ['backend'], frontend: ['frontend'], all: ['backend', 'frontend'] }[target];
  if (!parts || (fi >= 0 && !filter)) {
    process.stderr.write('Использование: node .claude/scripts/check.mjs backend|frontend|all [--no-tests] [--filter <выражение>]\n');
    process.exit(2);
  }
  const root = resolve(dirname(fileURLToPath(import.meta.url)), '..', '..');
  if (!existsSync(join(root, SOLUTION))) {
    process.stderr.write(`Не найден ${SOLUTION} относительно ${root}\n`);
    process.exit(2);
  }
  let ok = true;
  for (const part of parts) {
    const res = checkPart(root, part, withTests, filter);
    process.stdout.write(res.text + '\n');
    ok &&= res.ok;
  }
  process.exit(ok ? 0 : 1);
}

if (import.meta.url === pathToFileURL(process.argv[1] ?? '').href) main();
