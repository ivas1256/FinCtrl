from pydantic import BaseModel, Field


class PaymentSource(BaseModel):
    """ входные данные - источник платежа"""
    Id: int
    Name: str
