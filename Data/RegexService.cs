using System.Text.RegularExpressions;

namespace KriptoProje.Data;

public static class RegexService
{
    // 1. T.C. Kimlik No (11 rakam, ilk rakam 0 olamaz)
    public const string TcNoPattern = @"^[1-9][0-9]{10}$";

    // 2. Telefon (05xx xxx xx xx formatı)
    public const string PhonePattern = @"^05[0-9]{9}$";

    // 3. IBAN (TR ile başlar, 24 rakam devam eder)
    public const string IbanPattern = @"^TR[0-9]{24}$";

    // 4. Kredi Kartı (16 haneli rakam)
    public const string CreditCardPattern = @"^[0-9]{16}$";

    // 5. IPv4 Adresi (Standart IP formatı)
    public const string IpPattern = @"^((25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)$";
}