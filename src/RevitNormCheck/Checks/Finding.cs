namespace RevitNormCheck.Checks
{
    public static class Results
    {
        public const string Violation = "Нарушение";
        public const string NoData = "Нет данных";
        public const string NoRules = "Нет правил";
        public const string Unapproved = "Не утверждено";
        public const string RuleChanged = "Правило: проверить";
        public const string RuleMissing = "Правило: устарело";
        public const string NotVerified = "Правила не сверены";
        public const string NotImplemented = "Не реализовано";
        public const string Error = "Ошибка проверки";
    }

    public class Finding
    {
        public string CheckId { get; set; }
        public string CheckTitle { get; set; }
        public string Result { get; set; }
        public string Sheet { get; set; }
        public string View { get; set; }
        public string Level { get; set; }
        public string Element { get; set; }
        public int? ElementId { get; set; }
        public string Actual { get; set; }
        public string Required { get; set; }
        public string Document { get; set; }
        public string Clause { get; set; }
        public string Quote { get; set; }
        public string Comment { get; set; }
        public double? XMm { get; set; }
        public double? YMm { get; set; }
    }
}
