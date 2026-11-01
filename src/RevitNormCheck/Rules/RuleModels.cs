using System.Collections.Generic;
using System.Runtime.Serialization;

namespace RevitNormCheck.Rules
{
    [DataContract]
    public class RuleFile
    {
        [DataMember(Name = "rules")] public List<Rule> Rules { get; set; }
    }

    // Одно правило с цитатой и пунктом нормы. Значения в код не зашиты: их заносят в rules.json
    // по тексту норм после просмотра человеком.
    [DataContract]
    public class Rule
    {
        [DataMember(Name = "id")] public string Id { get; set; }
        [DataMember(Name = "check")] public string Check { get; set; }
        [DataMember(Name = "title")] public string Title { get; set; }

        // Подгруппа из списка проверок плагина, например "A.1".
        [DataMember(Name = "subgroup")] public string Subgroup { get; set; }

        // Раздел для удобства просмотра: «Пожарная безопасность», «Жилые здания» и т.д.
        // В Excel один раздел соответствует одной вкладке.
        [DataMember(Name = "section")] public string Section { get; set; }
        [DataMember(Name = "document")] public string Document { get; set; }

        // Редакция документа, например «2020, изм. 1». Правило относится только к ней.
        [DataMember(Name = "edition")] public string Edition { get; set; }
        [DataMember(Name = "clause")] public string Clause { get; set; }
        [DataMember(Name = "quote")] public string Quote { get; set; }

        // Контрольная сумма текста цитаты на момент составления правила.
        [DataMember(Name = "quoteHash")] public string QuoteHash { get; set; }

        // Минимальная ширина в мм (двери, марши).
        [DataMember(Name = "minWidthMm")] public double? MinWidthMm { get; set; }

        // К каким элементам относится правило.
        [DataMember(Name = "when")] public RuleWhen When { get; set; }

        // Для открывания дверей: направление эвакуации идёт из помещений с одним названием
        // в помещения с другим (например, из квартиры в коридор).
        [DataMember(Name = "fromRoomNameContains")] public List<string> FromRoomNameContains { get; set; }
        [DataMember(Name = "toRoomNameContains")] public List<string> ToRoomNameContains { get; set; }

        // Кто и когда утвердил правило. Без утверждения плагин правило не применяет.
        [DataMember(Name = "approvedBy")] public string ApprovedBy { get; set; }
        [DataMember(Name = "approvedOn")] public string ApprovedOn { get; set; }
    }

    [DataContract]
    public class RuleWhen
    {
        // Совпадение хотя бы одной подстроки без учёта регистра; пустой список означает «любые».
        [DataMember(Name = "roomNameContains")] public List<string> RoomNameContains { get; set; }
        [DataMember(Name = "typeNameContains")] public List<string> TypeNameContains { get; set; }
        [DataMember(Name = "levelContains")] public List<string> LevelContains { get; set; }
    }
}
