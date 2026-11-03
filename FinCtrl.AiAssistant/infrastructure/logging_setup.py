import logging
from pathlib import Path


def configure_logging(log_file: Path) -> None:
    """ пишет предупреждения и ошибки (с трейсом) в файл """
    log_file.parent.mkdir(parents=True, exist_ok=True)
    handler = logging.FileHandler(log_file, encoding="utf-8")
    handler.setFormatter(logging.Formatter("%(asctime)s %(levelname)s %(name)s: %(message)s"))
    handler.setLevel(logging.WARNING)

    root = logging.getLogger()
    root.setLevel(logging.WARNING)
    root.addHandler(handler)
