from sqlalchemy import create_engine
import pandas as pd
import sys

def truncate_all_tables(db_params):
    engine = create_engine(
        f"postgresql://{db_params['user']}:{db_params['password']}@"
        f"{db_params['host']}:{db_params['port']}/{db_params['dbname']}"
    )
    with engine.connect() as conn:
        result = conn.execute(text("""
            SELECT tablename FROM pg_tables WHERE schemaname = 'public';
        """))
        tables = [row[0] for row in result]
        if tables:
            truncate_stmt = f"TRUNCATE TABLE {', '.join(tables)} CASCADE;"
            conn.execute(text(truncate_stmt))
            print("Все таблицы успешно очищены.")
        else:
            print("В схеме public нет таблиц для очистки.")

def export_to_excel(output_file, db_params):
    try:
        engine = create_engine(
            f"postgresql://{db_params['user']}:{db_params['password']}@{db_params['host']}:{db_params['port']}/{db_params['dbname']}"
        )
        
        overall_query = """
            SELECT
                o.order_date,
                c.customer_name,
                c.customer_phone,
                p.product_name,
                cat.category_name AS product_category,
                p.product_price,
                od.quantity
            FROM order_details od
            JOIN orders o ON od.order_id = o.order_id
            JOIN customers c ON o.customer_id = c.customer_id
            JOIN products p ON od.product_id = p.product_id
            JOIN categories cat ON p.category_id = cat.category_id
            ORDER BY o.order_date;
        """
        
        tables_queries = {
            "Categories": "SELECT * FROM categories",
            "Products": "SELECT * FROM products",
            "Customers": "SELECT * FROM customers",
            "Orders": "SELECT * FROM orders",
            "Order_Details": "SELECT * FROM order_details"
        }
        
        with pd.ExcelWriter(output_file, engine="openpyxl") as writer:
            df_overall = pd.read_sql(overall_query, engine)
            df_overall.to_excel(writer, sheet_name="Overall", index=False)
            
            for sheet_name, query in tables_queries.items():
                df = pd.read_sql(query, engine)
                df.to_excel(writer, sheet_name=sheet_name, index=False)
        
        print(f"Данные успешно экспортированы в {output_file}")
        print(df_overall)

        truncate_all_tables(db_params)

    except Exception as e:
        print(f"Ошибка экспорта: {e}")

if __name__ == "__main__":
    if len(sys.argv) != 7:
        print("Использование: python export_to_excel.py <output_file> <dbname> <user> <password> <host> <port>")
        sys.exit(1)
    
    output_file = sys.argv[1]
    db_params = {
        "dbname": sys.argv[2],
        "user": sys.argv[3],
        "password": sys.argv[4],
        "host": sys.argv[5],
        "port": sys.argv[6]
    }
    
    export_to_excel(output_file, db_params)
