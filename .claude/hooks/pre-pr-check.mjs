// PreToolUse-хук Claude Code: автоматическая часть «Проверки перед PR» (Definition of Done)
// из .ai-docs/workflow/task-workflow.md, раздел 4. Срабатывает на `git push` / `gh pr create`.
// exit 0 — пропустить команду, exit 2 — заблокировать (stderr уходит агенту как причина).
import { spawnSync } from 'node:child_process';
import { existsSync, readFileSync, writeFileSync } from 'node:fs';
import { join } from 'node:path';
import { buildBackend, buildFrontend, hasBackendTests, hasFrontendTests, testBackend, testFrontend } from '../scripts/check.mjs';

const BASE = 'main';
const BACKEND = 'FinCtrl/';
const FRONTEND = 'FinCtrl.Frontend/';
const SUMMARY_DOC = '.ai-docs/project/project-summary.md';
// Правки только в этих путях не требуют папки задачи (регламент, вводная часть).
const DOCS_ONLY = [/^\.ai-docs\//, /^\.ai-tasks\//, /^CLAUDE\.md$/];
// Вендорные библиотеки legacy-MVC лежат в репозитории как есть.
const VENDORED = /\/wwwroot\/lib\//;
const BRANCH_RE = /^(feature|fix|refactor|chore)\/(\d{4})-[a-z0-9]+(-[a-z0-9]+)*$/;
// Только в позиции команды (начало строки или после ; & | ( ), чтобы не ловить `git push` внутри строк-данных.
const CMD_START = String.raw`(?:^|[;&|\n(])\s*`;
const PUSH_RE = new RegExp(CMD_START + String.raw`git(?:\s+-C\s+\S+|\s+-c\s+\S+|\s+--[\w-]+(?:=\S+)?)*\s+push\b[^;&|\n]*`, 'g');
const PR_RE = new RegExp(CMD_START + String.raw`gh\s+pr\s+create\b`);

let root = process.cwd();

function run(cmd, args, opts = {}) {
  const r = spawnSync(cmd, args, { cwd: root, encoding: 'utf8', shell: opts.shell ?? false, maxBuffer: 64 * 1024 * 1024 });
  return { code: r.status ?? 1, out: (r.stdout || '') + (r.stderr || '') };
}
const git = (...args) => run('git', args).out.trim();
const tail = (text, n = 40) => text.trim().split(/\r?\n/).slice(-n).join('\n');

function readInput() {
  try {
    return JSON.parse(readFileSync(0, 'utf8'));
  } catch {
    return null;
  }
}

function block(errors) {
  process.stderr.write(
    'Проверка перед PR (.claude/hooks/pre-pr-check.mjs) не пройдена — команда заблокирована.\n' +
    'Исправь проблемы, закоммить и повтори. Хук не обходить и не отключать.\n\n' +
    errors.map((e) => `• ${e}`).join('\n\n') + '\n'
  );
  process.exit(2);
}

// --- git-предусловия: дёшево, проверяются всегда ---
function checkPreconditions(command, pushes, branch) {
  const errors = [];
  if (pushes.some((p) => /\s(--force(-with-lease)?|-f)(\s|=|$)/.test(p)))
    errors.push('push --force запрещён регламентом (раздел 5).');
  if (/--no-verify\b/.test(command))
    errors.push('--no-verify запрещён регламентом.');
  if (branch === BASE || branch === 'HEAD')
    errors.push(`Текущая ветка «${branch}»: в ${BASE} напрямую не пушим и PR из неё не создаём — работай в ветке задачи.`);
  const dirty = git('status', '--porcelain');
  if (dirty)
    errors.push(`Рабочее дерево не чистое — проверяется не то, что уйдёт в PR. Закоммить или убери изменения:\n${tail(dirty, 20)}`);
  return errors;
}

// --- папка задачи и журнал ---
function checkTaskFolder(branch) {
  const m = BRANCH_RE.exec(branch);
  if (!m)
    return [`Имя ветки «${branch}» не по шаблону <тип>/<ID>-<slug> (тип: feature|fix|refactor|chore, ID — 4 цифры, slug — kebab-case).`];
  const errors = [];
  const id = m[2];
  const journal = join(root, '.ai-tasks/tasks.md');
  if (!existsSync(journal) || !new RegExp(`^\\|\\s*${id}\\s*\\|`, 'm').test(readFileSync(journal, 'utf8')))
    errors.push(`В журнале .ai-tasks/tasks.md нет строки задачи ${id}.`);
  const about = join(root, '.ai-tasks', branch, 'about.md');
  if (!existsSync(about)) {
    errors.push(`Нет файла постановки .ai-tasks/${branch}/about.md.`);
  } else {
    const unchecked = readFileSync(about, 'utf8').split(/\r?\n/).filter((l) => /^\s*-\s*\[ \]/.test(l));
    if (unchecked.length)
      errors.push(`В .ai-tasks/${branch}/about.md есть неотмеченные критерии приемки — выполни и отметь их:\n${unchecked.join('\n')}`);
  }
  if (!existsSync(join(root, '.ai-tasks', branch, 'plan.md')))
    errors.push(`Нет файла плана .ai-tasks/${branch}/plan.md.`);
  return errors;
}

// --- гигиена диффа: отладочный код, артефакты, секреты, отключённые тесты ---
function checkDiff(nameStatus) {
  const errors = [];
  for (const [status, file] of nameStatus) {
    if (VENDORED.test(file)) continue;
    if (status === 'A' && /(^|\/)(bin|obj|dist|node_modules)\//.test(file))
      errors.push(`Сгенерированный артефакт в диффе: ${file}`);
    if (status !== 'D' && (/(^|\/)\.env(\.|$)/.test(file) || /appsettings\.Production/i.test(file)))
      errors.push(`Файл с настройками не-dev окружения/секретами в диффе: ${file}`);
    if (status === 'D' && /(Tests?\/|\.spec\.|\.test\.)/.test(file))
      errors.push(`Удалён тестовый файл: ${file} — тесты не удалять ради зелёного прогона.`);
  }

  const patch = run('git', ['diff', '-U0', '--no-color', `${BASE}...HEAD`]).out;
  const findings = [];
  let file = '';
  for (const line of patch.split(/\r?\n/)) {
    if (line.startsWith('+++ ')) { file = line.replace(/^\+\+\+ (b\/)?/, ''); continue; }
    if (!line.startsWith('+') || VENDORED.test(file)) continue;
    const text = line.slice(1);
    const isJs = /\.(m?js|ts|vue)$/.test(file);
    const isCs = /\.cs$/.test(file);
    const isConfig = /\.(json|config|cs|m?js|ts|vue)$/.test(file);
    if (isJs && /\bconsole\.(log|debug)\s*\(/.test(text)) findings.push([file, 'отладочный console.log', text]);
    if (isJs && /^\s*debugger\s*;?\s*$/.test(text)) findings.push([file, 'debugger', text]);
    if (isCs && /\bDebugger\.(Break|Launch)\s*\(/.test(text)) findings.push([file, 'Debugger.Break', text]);
    if (isConfig && /\b(Password|Pwd)\s*=\s*[^;"'\s{]/i.test(text)) findings.push([file, 'пароль в строке подключения', text]);
    if (isCs && /\bSkip\s*=\s*"/.test(text)) findings.push([file, 'отключённый тест (Skip)', text]);
    if (isJs && /\b(it|test|describe)\.(skip|only)\s*\(/.test(text)) findings.push([file, 'отключённый/изолированный тест (.skip/.only)', text]);
  }
  if (findings.length)
    errors.push('Подозрительные строки в диффе:\n' + findings.map(([f, what, t]) => `  ${f}: ${what}: ${t.trim()}`).join('\n'));
  return errors;
}

// --- сборка и тесты затронутых частей (общая логика с .claude/scripts/check.mjs) ---
const report = (what, step) => `${what} упал:\n${step.details.join('\n')}`;

function checkBackend(changed, notes) {
  const errors = [];
  // --no-incremental: иначе предупреждения в уже собранных проектах не выводятся.
  const build = buildBackend(root, { noIncremental: true });
  if (!build.ok) return [report('dotnet build', build)];

  const changedCs = changed.filter((f) => f.startsWith(BACKEND)).map((f) => f.toLowerCase());
  const warnings = new Set();
  for (const line of build.out.split(/\r?\n/)) {
    const m = /^\s*(.+?)\(\d+,\d+\): warning /.exec(line);
    if (!m) continue;
    const path = m[1].replace(/\\/g, '/').toLowerCase();
    if (changedCs.some((f) => path.endsWith(f))) warnings.add(line.replace(/\s*\[[^\]]*\]\s*$/, '').trim());
  }
  if (warnings.size)
    errors.push(`Предупреждения сборки в изменённых файлах:\n${[...warnings].join('\n')}`);

  if (hasBackendTests(root)) {
    const test = testBackend(root);
    if (!test.ok) errors.push(report('dotnet test', test));
    else notes.push(`backend: build + ${test.summary}`);
  } else {
    notes.push('backend: build (тестового проекта в .sln пока нет)');
  }
  return errors;
}

function checkFrontend(notes) {
  const errors = [];
  const build = buildFrontend(root);
  if (!build.ok) return [report('npm run build', build)];
  if (hasFrontendTests(root)) {
    const test = testFrontend(root);
    if (!test.ok) errors.push(report('npm run test', test));
    else notes.push(`frontend: build + ${test.summary}`);
  } else {
    notes.push('frontend: build (скрипта test пока нет)');
  }
  return errors;
}

function main() {
  const input = readInput();
  const command = input?.tool_input?.command;
  if (typeof command !== 'string') process.exit(0);
  const pushes = (command.match(PUSH_RE) || []).filter((p) => !/\s(--delete|-d)\b/.test(p));
  if (!pushes.length && !PR_RE.test(command)) process.exit(0);

  root = process.env.CLAUDE_PROJECT_DIR || input.cwd || root;
  root = git('rev-parse', '--show-toplevel') || root;
  const branch = git('rev-parse', '--abbrev-ref', 'HEAD');

  const pre = checkPreconditions(command, pushes, branch);
  if (pre.length) block(pre);

  const head = git('rev-parse', 'HEAD');
  const cacheFile = join(root, git('rev-parse', '--git-dir'), 'finctrl-dod-ok');
  if (existsSync(cacheFile) && readFileSync(cacheFile, 'utf8').trim() === head) process.exit(0);

  const nameStatus = git('diff', '--name-status', '--no-renames', `${BASE}...HEAD`)
    .split(/\r?\n/).filter(Boolean).map((l) => l.split('\t'));
  const changed = nameStatus.map(([, f]) => f);
  if (!changed.length) block([`Ветка не отличается от ${BASE} — нечего пушить в PR.`]);

  const docsOnly = changed.every((f) => DOCS_ONLY.some((re) => re.test(f)));
  const touchesBackend = changed.some((f) => f.startsWith(BACKEND));
  const touchesFrontend = changed.some((f) => f.startsWith(FRONTEND));

  const errors = [...checkDiff(nameStatus)];
  if (!docsOnly) errors.push(...checkTaskFolder(branch));
  const notes = [];
  if (touchesBackend) errors.push(...checkBackend(changed, notes));
  if (touchesFrontend) errors.push(...checkFrontend(notes));
  if (errors.length) block(errors);

  writeFileSync(cacheFile, head + '\n');
  const reminders = ['ручная проверка в preview (п.3) и смысловая самопроверка диффа (п.5) — на тебе'];
  if ((touchesBackend || touchesFrontend) && !changed.includes(SUMMARY_DOC))
    reminders.push(`${SUMMARY_DOC} не менялся — обнови, если изменения существенные (п.6)`);
  process.stdout.write(JSON.stringify({
    systemMessage: `DoD-хук пройден${notes.length ? ` (${notes.join('; ')})` : ''}. Напоминание: ${reminders.join('; ')}.`,
  }));
  process.exit(0);
}

try {
  main();
} catch (e) {
  block([`Хук упал с ошибкой: ${e?.stack || e}`]);
}
