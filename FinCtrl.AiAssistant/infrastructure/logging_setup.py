import logging
import sys
from datetime import date
from pathlib import Path

LOG_FORMAT = "%(asctime)s %(levelname)s %(name)s: %(message)s"

# логгеры uvicorn не передают записи в корневой (propagate=False) — файл подключается к ним отдельно
UVICORN_LOGGERS = ("uvicorn", "uvicorn.access")


class DailyFileHandler(logging.FileHandler):
    """ пишет в <log_dir>/yyyymmdd.log и переключается на новый файл при смене даты """

    def __init__(self, log_dir: Path):
        self.log_dir = log_dir
        self.current_date = date.today()
        log_dir.mkdir(parents=True, exist_ok=True)
        super().__init__(self._path_for(self.current_date), encoding="utf-8", delay=True)

    def _path_for(self, day: date) -> Path:
        return self.log_dir / f"{day:%Y%m%d}.log"

    def emit(self, record: logging.LogRecord) -> None:
        today = date.today()
        if today != self.current_date:
            self.acquire()
            try:
                self.close()
                self.current_date = today
                self.baseFilename = str(self._path_for(today).resolve())
            finally:
                self.release()
        super().emit(record)


_configured = False


def configure_logging(log_dir: Path, level: int = logging.INFO) -> None:
    """ пишет логи в консоль и в файл <log_dir>/yyyymmdd.log; повторный вызов ничего не делает """
    global _configured
    if _configured:
        return
    _configured = True

    formatter = logging.Formatter(LOG_FORMAT)

    file_handler = DailyFileHandler(log_dir)
    file_handler.setFormatter(formatter)

    console_handler = logging.StreamHandler(sys.stdout)
    console_handler.setFormatter(formatter)

    root = logging.getLogger()
    root.setLevel(level)
    root.addHandler(file_handler)
    root.addHandler(console_handler)

    for name in UVICORN_LOGGERS:
        uvicorn_logger = logging.getLogger(name)
        if not uvicorn_logger.propagate:
            uvicorn_logger.addHandler(file_handler)
