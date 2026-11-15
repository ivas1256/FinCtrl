from functools import lru_cache
from pathlib import Path

from pydantic import SecretStr
from pydantic_settings import BaseSettings, SettingsConfigDict

BASE_DIR = Path(__file__).parent.parent


class Settings(BaseSettings):
    """ настройки из переменных окружения или файла .env """
    model_config = SettingsConfigDict(env_file=BASE_DIR / ".env", env_file_encoding="utf-8")

    llm_api_key: SecretStr
    llm_base_url: str = "https://api.orcarouter.ai/v1"
    llm_model: str = "deepseek/deepseek-v4-flash-free"
    log_dir: Path = BASE_DIR / "logs"


@lru_cache
def get_settings() -> Settings:
    return Settings()
