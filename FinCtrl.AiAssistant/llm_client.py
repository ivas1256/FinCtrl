import openai
from openai import OpenAI

from config import Settings


class LlmError(Exception):
    """ запрос к LLM не удался """


class LlmClient:
    """ отправка запроса к LLM через OpenAI-совместимый API """

    def __init__(self, settings: Settings, client: OpenAI | None = None):
        self._model = settings.llm_model
        self._client = client or OpenAI(
            base_url=settings.llm_base_url,
            api_key=settings.llm_api_key.get_secret_value(),
        )

    def complete(self, system_prompt: str, user_content: str) -> str:
        """ возвращает текст ответа модели (ожидается JSON) """
        try:
            response = self._client.chat.completions.create(
                model=self._model,
                temperature=0,
                response_format={"type": "json_object"},
                messages=[
                    {"role": "system", "content": system_prompt},
                    {"role": "user", "content": user_content},
                ],
            )
        except openai.OpenAIError as e:
            raise LlmError(f"Запрос к LLM не удался: {e}") from e

        if not response.choices or not response.choices[0].message.content:
            raise LlmError("LLM вернула пустой ответ")

        return response.choices[0].message.content
