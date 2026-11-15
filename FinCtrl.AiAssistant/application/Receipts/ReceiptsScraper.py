import decimal
import logging
import datetime as dt
import re

from decimal import Decimal
from datetime import datetime
from typing import List
from lxml import html

from imap_tools import MailBox, AND
from infrastructure.config import get_settings
from infrastructure.logging_setup import configure_logging

settings = get_settings()
configure_logging(settings.log_dir)
logger = logging.getLogger("ai_assistant")

class ReceiptItem:
    Name: str
    Price: Decimal
    Quantity: Decimal
    MeasureUnitName: str
    PricePerUnit: Decimal

class Receipt:
    Name: str
    Date: dt.datetime
    TotalPrice: Decimal
    Items: List[ReceiptItem]


class ReceiptsEmailScraper:

    def __init__(self):
        pass

    def get_emails(self) -> list[Receipt]:
        receipts = self.__parse_receipts_list()

        for receipt in receipts:



    def __parse_receipts_list(self) -> list[Receipt]:
        try:
            with MailBox('imap.yandex.ru').login('ivas1256@yandex.ru', 'mfqfwwqizqlowrnr') as mailbox:
                mailbox.folder.set('5ка')

                criteria = AND(date_gte=dt.date(2026, 9, 1), date_lt=dt.date(2026, 12, 31))
                for msg in mailbox.fetch(criteria, mark_seen=False):
                    body_html = html.fromstring(msg.html)
                    raw_date = body_html.xpath("//td[normalize-space(.)='Дата | Время']/following-sibling::td[1]")[0].text_content().strip()
                    raw_total_price = body_html.xpath("//td[normalize-space(.)='Итог']/following-sibling::td[1]")[0].text_content().strip()

                    receipt = Receipt()
                    receipt.Name = msg.subject
                    receipt.Date = datetime.strptime(raw_date.strip(), "%d.%m.%Y | %H:%M")
                    receipt.TotalPrice = Decimal(raw_total_price)
                    receipt.Items = []

                    xpath_name = ("//td[normalize-space(.)='КАССОВЫЙ ЧЕК']/following::td[@width='100%' "
                            "and preceding-sibling::td[1][contains(., '.')] "
                            "and following::td[normalize-space(.)='Итог']]")
                    xpath_price = "normalize-space(following::td[normalize-space(.)='Цена * Кол'][1]/following-sibling::td[1])"

                    for td in body_html.xpath(xpath_name):
                        receipt_item = ReceiptItem()
                        receipt_item.Name = td.text_content().strip()

                        raw_price = td.xpath(xpath_price)
                        tmp = self.__parse_price_row(raw_price)
                        receipt_item.PricePerUnit = tmp.PricePerUnit
                        receipt_item.Quantity = tmp.Quantity
                        receipt_item.MeasureUnitName = tmp.MeasureUnitName
                        receipt_item.Price = tmp.Price

                        receipt.Items.append(receipt_item)

                    items_lines = "\n".join(
                        f"    - {item.Name}: {item.Quantity} {item.MeasureUnitName} × {item.PricePerUnit} = {item.Price}"
                        for item in receipt.Items
                    )
                    logger.info(
                        f"Чек: {receipt.Name}\n"
                        f"  Дата: {receipt.Date:%d.%m.%Y %H:%M}\n"
                        f"  Сумма: {receipt.TotalPrice}\n"
                        f"  Позиции ({len(receipt.Items)}):\n"
                        f"{items_lines}"
                    )

        except Exception as e:
            logger.error(e)
            raise e

    def __parse_price_row(self, raw_price: str) -> ReceiptItem:
        price_rgx = re.compile(
            r"^\s*(?P<unit_price>\d+(?:[.,]\d+)?)\s*\*\s*"
            r"(?P<quantity>\d+(?:[.,]\d+)?)\s*"
            r"(?P<unit>[^\d\s=]+)?\s*=\s*"
            r"(?P<total>\d+(?:[.,]\d+)?)\s*$"
        )
        match = price_rgx.match(raw_price)
        receipt_item = ReceiptItem()

        try:
            receipt_item.PricePerUnit = Decimal(match["unit_price"])
            receipt_item.Quantity = Decimal(match["quantity"])
            receipt_item.MeasureUnitName = str(match["unit"])
            receipt_item.Price = Decimal(match["total"])

            return  receipt_item
        except Exception as e:
            logger.error(f"error parsing price row: {raw_price}")
            raise e
