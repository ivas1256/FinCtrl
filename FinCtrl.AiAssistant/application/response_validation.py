import json
import re

from pydantic import TypeAdapter, ValidationError

from models import ClassifiedPaymentSource, PaymentSource


class LlmResponseValidationError(Exception):
    """ ответ LLM не прошёл проверку """


_CODE_FENCE = re.compile(r"^```(?:json)?\s*(.*?)\s*```$", re.DOTALL)
_items_adapter = TypeAdapter(list[ClassifiedPaymentSource])


def _extract_items(raw: str) -> object:
    text = raw.strip()
    fence = _CODE_FENCE.match(text)
    if fence:
        text = fence.group(1)

    try:
        data = json.loads(text)
    except json.JSONDecodeError as e:
        raise LlmResponseValidationError(f"Ответ LLM не является JSON: {e}") from e

    # в режиме json_object модель возвращает объект: промт просит {"Items": [...]}; голый массив тоже принимаем
    if isinstance(data, dict):
        lists = [value for value in data.values() if isinstance(value, list)]
        if len(lists) != 1:
            raise LlmResponseValidationError("Ответ LLM — объект без единственного поля-списка")
        data = lists[0]

    return data


def parse_classification(raw: str, sources: list[PaymentSource]) -> list[ClassifiedPaymentSource]:
    """ разбирает и проверяет ответ LLM; Name берётся из входных данных, порядок — как во входе """
    try:
        items = _items_adapter.validate_python(_extract_items(raw))
    except ValidationError as e:
        raise LlmResponseValidationError(f"Ответ LLM не соответствует схеме: {e}") from e

    classifications: dict[int, str] = {}
    for item in items:
        if item.Id in classifications:
            raise LlmResponseValidationError(f"Ответ LLM содержит Id {item.Id} несколько раз")
        if not item.Classification.strip():
            raise LlmResponseValidationError(f"Ответ LLM содержит пустую классификацию для Id {item.Id}")
        classifications[item.Id] = item.Classification.strip()

    expected_ids = {s.Id for s in sources}
    missing = expected_ids - classifications.keys()
    extra = classifications.keys() - expected_ids
    if missing or extra:
        raise LlmResponseValidationError(
            f"Id в ответе LLM не совпадают с запросом: нет {sorted(missing)}, лишние {sorted(extra)}")

    return [ClassifiedPaymentSource(Id=s.Id, Name=s.Name, Classification=classifications[s.Id]) for s in sources]
