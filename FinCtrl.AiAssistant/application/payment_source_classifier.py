import json

from infrastructure.config import BASE_DIR
from infrastructure.llm_client import LlmClient
from models import classified_payment_source, payment_source
from application.response_validation import LlmResponseValidationError, parse_classification

PROMTS_DIR = BASE_DIR / "promts"


def read_promt(name: str) -> str:
    return (PROMTS_DIR / name).read_text(encoding="utf-8")


class PaymentSourceClassifier:
    """ классификация источников платежей через LLM """

    def __init__(self, llm_client: LlmClient):
        self._llm_client = llm_client

    def classify(self, sources: list[payment_source]) -> list[classified_payment_source]:
        raw = self._llm_client.complete(
            system_prompt=read_promt("classify-payment-sources.md"),
            user_content=json.dumps([s.model_dump() for s in sources], ensure_ascii=False),
        )
        try:
            return parse_classification(raw, sources)
        except LlmResponseValidationError as e:
            # сырой ответ модели нужен в логе для разбора
            raise LlmResponseValidationError(f"{e}. Ответ LLM: {raw[:1000]}") from e
