from pydantic import BaseModel, Field


class PaymentSource(BaseModel):
    """ входные данные - источник платежа"""
    Id: int
    Name: str


class ClassifiedPaymentSource(BaseModel):
    """ вЫходные данные - классифицированный источник платежа"""
    Id: int
    Name: str
    Classification: str = Field(min_length=1)
