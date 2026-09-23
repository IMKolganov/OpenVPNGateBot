using DataGateMonitor.SharedModels.Enums;

namespace DataGateVPNBot.Localization;

public static class DonateTextFallbacks
{
    public static string Get(string key, Language language)
    {
        var lang = language switch
        {
            Language.Russian => "ru",
            Language.Greek => "el",
            _ => "en"
        };

        return (key, lang) switch
        {
            ("DonateIntro", "ru") =>
                "Если DataGate вам помогает — добровольная поддержка Telegram Stars. Тариф VPN не меняется.\n\nНажмите сумму ⭐ — оплата внутри Telegram.\nНижний ряд USDT — только если баланс в @CryptoBot уже есть.",
            ("DonateIntro", "el") =>
                "Αν το DataGate σας βοηθά, μπορείτε να στείλετε υποστήριξη με Telegram Stars. Το πλάνο VPN δεν αλλάζει.\n\nΠατήστε ποσό ⭐ — πληρωμή μέσα στο Telegram.\nUSDT μόνο αν έχετε ήδη υπόλοιπο στο @CryptoBot.",
            ("DonateIntro", _) =>
                "If DataGate helps you, you can send voluntary support with Telegram Stars. VPN plan does not change.\n\nTap a ⭐ amount — pay inside Telegram.\nUSDT row is only if you already have a @CryptoBot balance.",

            ("DonateStarsTitle", "ru") => "Поддержка DataGate",
            ("DonateStarsTitle", "el") => "Υποστήριξη DataGate",
            ("DonateStarsTitle", _) => "Support DataGate",

            ("DonateStarsDescription", "ru") => "Добровольная поддержка. Тариф VPN не меняется.",
            ("DonateStarsDescription", "el") => "Εθελοντική υποστήριξη. Το πλάνο VPN δεν αλλάζει.",
            ("DonateStarsDescription", _) => "Voluntary support. VPN plan does not change.",

            ("DonatePayButton", "ru") => "Оплатить {amount} USD",
            ("DonatePayButton", "el") => "Πληρωμή {amount} USD",
            ("DonatePayButton", _) => "Pay {amount} USD",

            ("DonateInvoiceCreated", "ru") =>
                "Счёт на {amount} USD готов. Оплата из уже имеющегося баланса @CryptoBot.",
            ("DonateInvoiceCreated", "el") =>
                "Το τιμολόγιο για {amount} USD είναι έτοιμο. Πληρωμή από υπάρχον υπόλοιπο @CryptoBot.",
            ("DonateInvoiceCreated", _) =>
                "Invoice for {amount} USD is ready. Pay from an existing @CryptoBot balance.",

            ("DonateThanks", "ru") => "Спасибо за {amount} {asset}! 💚",
            ("DonateThanks", "el") => "Ευχαριστούμε για {amount} {asset}! 💚",
            ("DonateThanks", _) => "Thank you for {amount} {asset}! 💚",

            ("DonateDisabled", "ru") => "Донаты сейчас выключены.",
            ("DonateDisabled", "el") => "Οι δωρεές είναι απενεργοποιημένες.",
            ("DonateDisabled", _) => "Donations are turned off right now.",

            ("DonateInvoiceFailed", "ru") => "Не удалось создать счёт. Попробуйте позже.",
            ("DonateInvoiceFailed", "el") => "Δεν ήταν δυνατή η δημιουργία τιμολογίου. Δοκιμάστε ξανά αργότερα.",
            ("DonateInvoiceFailed", _) => "Could not create a payment invoice. Please try again later.",

            ("DonateCryptoRiskBanner", "ru") =>
                "<b>⚠️ Внимание: риск!!!</b>\n\n" +
                "Вы выбрали поддержку через @CryptoBot на {amount} USD.\n\n" +
                "Пополнение @CryptoBot во многих странах идёт через P2P — перевод от человека человеку. Это может повлечь для вас юридические и финансовые риски и проблемы.\n\n" +
                "Через P2P вы можете невольно участвовать в схеме с чужими или крадеными деньгами либо в другом финансовом или уголовном деле.\n\n" +
                "Если вы недостаточно осведомлены об этом способе оплаты — проигнорируйте его. Используйте @CryptoBot только на свой страх и риск.\n\n" +
                "Продолжайте только если баланс в @CryptoBot у вас уже есть и вы понимаете эти риски. Тариф VPN от доната не меняется.",
            ("DonateCryptoRiskBanner", "el") =>
                "<b>⚠️ Προσοχή: κίνδυνος!!!</b>\n\n" +
                "Επιλέξατε υποστήριξη μέσω @CryptoBot για {amount} USD.\n\n" +
                "Σε πολλές χώρες η φόρτιση του @CryptoBot γίνεται με P2P — μεταφορά από άνθρωπο σε άνθρωπο. Αυτό μπορεί να σας εκθέσει σε νομικούς και οικονομικούς κινδύνους.\n\n" +
                "Μέσω P2P μπορείτε άθελά σας να συμμετάσχετε σε σχήμα με χρήματα τρίτων ή κλεμμένα χρήματα, ή σε άλλη οικονομική ή ποινική υπόθεση.\n\n" +
                "Αν δεν γνωρίζετε αρκετά αυτόν τον τρόπο πληρωμής, αγνοήστε τον. Χρησιμοποιήστε το @CryptoBot αποκλειστικά με δική σας ευθύνη.\n\n" +
                "Συνεχίστε μόνο αν έχετε ήδη υπόλοιπο στο @CryptoBot και κατανοείτε τους κινδύνους. Το πλάνο VPN δεν αλλάζει.",
            ("DonateCryptoRiskBanner", _) =>
                "<b>⚠️ Attention: risk!!!</b>\n\n" +
                "You chose support via @CryptoBot for {amount} USD.\n\n" +
                "In many countries, topping up @CryptoBot is done through P2P — a person-to-person transfer. That can expose you to legal and financial risks and problems.\n\n" +
                "Through P2P you may unwittingly take part in a scheme involving other people's or stolen money, or in other financial or criminal activity.\n\n" +
                "If you are not sufficiently informed about this payment method, please ignore it. Use @CryptoBot only at your own risk.\n\n" +
                "Continue only if you already have a @CryptoBot balance and you understand these risks. A donation does not change the VPN plan.",

            ("DonateCryptoRiskContinue", "ru") => "Понимаю риск, продолжить",
            ("DonateCryptoRiskContinue", "el") => "Κατανοώ τον κίνδυνο, συνέχεια",
            ("DonateCryptoRiskContinue", _) => "I understand the risk, continue",

            ("DonateCryptoRiskIgnore", "ru") => "Игнорировать этот способ",
            ("DonateCryptoRiskIgnore", "el") => "Αγνόηση αυτής της μεθόδου",
            ("DonateCryptoRiskIgnore", _) => "Ignore this method",

            _ => key
        };
    }
}
