import logging

from fastapi import Depends, FastAPI, Request
from fastapi.exception_handlers import request_validation_exception_handler
from fastapi.exceptions import RequestValidationError
from fastapi.responses import JSONResponse

from application.payment_source_classifier import PaymentSourceClassifier
from infrastructure.config import get_settings
from infrastructure.llm_client import LlmClient
from infrastructure.logging_setup import configure_logging
from models import ClassifiedPaymentSource, PaymentSource

from application.Receipts.ReceiptsScraper import ReceiptsEmailScraper

settings = get_settings()
configure_logging(settings.log_dir)
logger = logging.getLogger("ai_assistant")

s = ReceiptsEmailScraper()

s.get_emails()

app = FastAPI(title="FinCtrl.AiAssistant")
#
#
# def get_classifier() -> PaymentSourceClassifier:
#     return PaymentSourceClassifier(LlmClient(settings))
#
#
# @app.exception_handler(RequestValidationError)
# async def log_request_validation_error(request: Request, exc: RequestValidationError):
#     logger.warning("Невалидный запрос %s %s: %s", request.method, request.url.path, exc.errors())
#     return await request_validation_exception_handler(request, exc)
#
#
# @app.exception_handler(Exception)
# async def handle_unexpected_error(request: Request, exc: Exception):
#     logger.exception("Ошибка при обработке %s %s", request.method, request.url.path, exc_info=exc)
#     return JSONResponse(status_code=500, content={"detail": "Внутренняя ошибка сервиса"})
#
#
# @app.post("/payment-sources/classify")
# def classify_payment_sources(
#         sources: list[PaymentSource],
#         classifier: PaymentSourceClassifier = Depends(get_classifier),
# ) -> list[ClassifiedPaymentSource]:
#     if not sources:
#         return []
#     return classifier.classify(sources)
#
