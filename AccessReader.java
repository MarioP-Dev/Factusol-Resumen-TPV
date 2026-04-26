import java.sql.Connection;
import java.sql.DriverManager;
import java.sql.ResultSet;
import java.sql.ResultSetMetaData;
import java.sql.Statement;

/**
 * Lee una base Microsoft Access vía UCanAccess y emite el resultado en CSV (stdout, con cabecera).
 * Uso: java -cp <classpath> AccessReader <ruta_db> "<query SQL>"
 */
public class AccessReader {

    public static void main(String[] args) throws Exception {
        if (args.length < 2) {
            System.err.println("Uso: AccessReader <ruta_db> <query_sql>");
            System.exit(1);
        }
        String dbPath = args[0];
        String query = args[1];

        String url = "jdbc:ucanaccess://" + dbPath;
        Class.forName("net.ucanaccess.jdbc.UcanaccessDriver");

        try (Connection conn = DriverManager.getConnection(url);
                Statement stmt = conn.createStatement();
                ResultSet rs = stmt.executeQuery(query)) {
            ResultSetMetaData meta = rs.getMetaData();
            int colCount = meta.getColumnCount();
            StringBuilder header = new StringBuilder();
            for (int c = 1; c <= colCount; c++) {
                if (c > 1) {
                    header.append(',');
                }
                header.append(escapeCsv(meta.getColumnLabel(c)));
            }
            System.out.println(header);
            while (rs.next()) {
                StringBuilder row = new StringBuilder();
                for (int c = 1; c <= colCount; c++) {
                    if (c > 1) {
                        row.append(',');
                    }
                    Object o = rs.getObject(c);
                    String cell = o == null ? "" : String.valueOf(o);
                    row.append(escapeCsv(cell));
                }
                System.out.println(row);
            }
            System.out.flush();
        }
    }

    private static String escapeCsv(String s) {
        if (s == null) {
            return "";
        }
        boolean needQuote =
                s.indexOf(',') >= 0
                        || s.indexOf('"') >= 0
                        || s.indexOf('\n') >= 0
                        || s.indexOf('\r') >= 0;
        if (!needQuote) {
            return s;
        }
        return '"' + s.replace("\"", "\"\"") + '"';
    }
}
