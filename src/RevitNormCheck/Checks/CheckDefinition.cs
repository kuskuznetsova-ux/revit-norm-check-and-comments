using System.Collections.Generic;

namespace RevitNormCheck.Checks
{
    public enum CheckCategory
    {
        Norms,
        CompanyStandards,
        Documentation
    }

    public class CheckDefinition
    {
        public string Id { get; }
        public string Title { get; }
        public CheckCategory Category { get; }

        public CheckDefinition(string id, string title, CheckCategory category)
        {
            Id = id;
            Title = title;
            Category = category;
        }

        // Нумерация совпадает с docs/RULES.md.
        public static IReadOnlyList<CheckDefinition> All { get; } = new List<CheckDefinition>
        {
            // A. Нормы
            new CheckDefinition("door-width", "A1. Ширина эвакуационных дверей в свету", CheckCategory.Norms),
            new CheckDefinition("corridor-width", "A2. Ширина коридоров на путях эвакуации", CheckCategory.Norms),
            new CheckDefinition("stair-width", "A3. Ширина маршей и площадок лестниц", CheckCategory.Norms),
            new CheckDefinition("stair-step", "A4. Высота и ширина ступени, уклон марша", CheckCategory.Norms),
            new CheckDefinition("door-swing", "A5. Открывание эвакуационных дверей по направлению эвакуации", CheckCategory.Norms),
            new CheckDefinition("exits-count", "A6. Число эвакуационных выходов", CheckCategory.Norms),
            new CheckDefinition("escape-length", "A7. Длина пути эвакуации", CheckCategory.Norms),
            new CheckDefinition("dead-end", "A8. Тупиковые коридоры", CheckCategory.Norms),
            new CheckDefinition("fire-rating", "A9. Предел огнестойкости стен, перекрытий, лестничных клеток", CheckCategory.Norms),
            new CheckDefinition("building-class", "A10. Степень огнестойкости и класс пожарной опасности здания", CheckCategory.Norms),
            new CheckDefinition("fire-doors", "A11. Противопожарные двери и тамбур-шлюзы", CheckCategory.Norms),
            new CheckDefinition("clear-height", "A12. Высота в свету на путях эвакуации", CheckCategory.Norms),
            new CheckDefinition("accessibility", "A13. Доступность МГН", CheckCategory.Norms),
            new CheckDefinition("parking", "A14. Паркинг: рампы, выезды, выходы", CheckCategory.Norms),
            new CheckDefinition("zones", "A15. Разделение жилья, ДОО и ритейла", CheckCategory.Norms),

            // B. Стандарты компании
            new CheckDefinition("std-naming", "B1. Именование видов, листов, семейств и типов", CheckCategory.CompanyStandards),
            new CheckDefinition("std-parameters", "B2. Обязательные параметры проекта и общие параметры", CheckCategory.CompanyStandards),
            new CheckDefinition("std-templates", "B3. Шаблоны видов, стили линий, штриховки, текст", CheckCategory.CompanyStandards),
            new CheckDefinition("std-worksets", "B4. Рабочие наборы и структура модели", CheckCategory.CompanyStandards),
            new CheckDefinition("std-sheets", "B5. Оформление листов и штампа по стандарту компании", CheckCategory.CompanyStandards),

            // C. Состав и оформление (АР, стадия П)
            new CheckDefinition("doc-sheets-set", "C1. Состав листов раздела АР (стадия П)", CheckCategory.Documentation),
            new CheckDefinition("doc-title-block", "C2. Заполнение основной надписи (шифр, стадия, наименования)", CheckCategory.Documentation),
            new CheckDefinition("doc-plan-elements", "C3. Обязательные элементы на планах (оси, отметки, размеры, номера помещений)", CheckCategory.Documentation),
            new CheckDefinition("doc-sections-facades", "C4. Разрезы и фасады: отметки, размеры, обозначения", CheckCategory.Documentation),
            new CheckDefinition("doc-explication", "C5. Экспликации и ведомости помещений, площади", CheckCategory.Documentation),
            new CheckDefinition("doc-scales-annotations", "C6. Масштабы, выноски, марки, условные обозначения", CheckCategory.Documentation),
        };
    }
}
