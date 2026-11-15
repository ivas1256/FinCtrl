import mssql_python
from application.Receipts import Receipt

class ReceiptRepository:
    def __init__(self):
        pass

    def batch_save(self, receipts):
        conn_str = ""
        conn = mssql_python.connect(conn_str)

        conn.execute("")