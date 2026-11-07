// PreToolUse-хук Claude Code: запрещает прямой запуск сборки и тестов (dotnet build/test, npm run build/test,
// vitest, vite build) из Bash/PowerShell. Их полный вывод забивает контекст агента — вместо них
// `node .claude/scripts/check.mjs backend|frontend|all`: одна строка при успехе, суть ошибок при провале.
// Сам check.mjs и хук pre-pr-check.mjs запускают dotnet/npm изнутри node — этот хук их не видит.
// exit 0 — пропустить команду, exit 2 — заблокировать (stderr уходит агенту как причина).
import { readFileSync } from 'node:fs';

// Только в позиции команды (начало строки или после ; & | ( ), чтобы не ловить упоминания внутри строк-данных.
const CMD_START = String.raw`(?:^|[;&|\n(])\s*`;
const RAW_BUILD = [
  String.raw`dotnet(?:\.exe)?\s+(?:build|test)\b`,
  String.raw`npm(?:\.cmd)?\b[^;&|\n]*?\s(?:run(?:-script)?\s+(?:build|test)|test|t)\b`,
  String.raw`(?:npx\s+)?vitest\b`,
  String.raw`(?:npx\s+)?vite\s+build\b`,
].map((p) => new RegExp(CMD_START + p, 'i'));

let command;
try {
  command = JSON.parse(readFileSync(0, 'utf8'))?.tool_input?.command;
} catch {
  process.exit(0);
}
if (typeof command !== 'string') process.exit(0);
// Содержимое кавычек — данные (шаблоны grep, сообщения коммитов, here-strings), а не команды.
const code = command.replace(/'[^']*'|"(?:\\.|[^"\\])*"/g, "''");
if (!RAW_BUILD.some((re) => re.test(code))) process.exit(0);

process.stderr.write(
  'Прямой запуск сборки/тестов запрещён (.claude/hooks/guard-raw-builds.mjs): полный вывод забивает контекст.\n' +
  'Используй `node .claude/scripts/check.mjs backend|frontend|all` (флаг --no-tests — только сборка) ' +
  'или агента build-runner. Полный лог последнего прогона — .ai-tmp/check-<часть>.log (читать Grep, не целиком).\n'
);
process.exit(2);
