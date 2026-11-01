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
        public string SubGroup { get; }

        public CheckDefinition(string id, string title, CheckCategory category, string subGroup = null)
        {
            Id = id;
            Title = title;
            Category = category;
            SubGroup = subGroup;
        }

        private const string Fire = "A.1 Пожарная безопасность";
        private const string Residential = "A.2 Жилые здания";
        private const string Public = "A.3 Общественные здания";
        private const string Mgn = "A.4 МГН";
        private const string Parking = "A.5 Стоянки";


        // Нумерация совпадает с docs/RULES.md.
        public static IReadOnlyList<CheckDefinition> All { get; } = new List<CheckDefinition>
        {
            // A.1 Пожарные нормы
            new CheckDefinition("door-width", "A.1.1 Ширина эвакуационных дверей в свету", CheckCategory.Norms, Fire),
            new CheckDefinition("corridor-width", "A.1.2 Ширина коридоров на путях эвакуации", CheckCategory.Norms, Fire),
            new CheckDefinition("stair-width", "A.1.3 Ширина маршей и площадок лестниц", CheckCategory.Norms, Fire),
            new CheckDefinition("door-swing", "A.1.4 Открывание эвакуационных дверей по направлению эвакуации", CheckCategory.Norms, Fire),
            new CheckDefinition("exits-count", "A.1.5 Число эвакуационных выходов", CheckCategory.Norms, Fire),
            new CheckDefinition("escape-length", "A.1.6 Длина пути эвакуации", CheckCategory.Norms, Fire),
            new CheckDefinition("dead-end", "A.1.7 Тупиковые коридоры", CheckCategory.Norms, Fire),
            new CheckDefinition("fire-rating", "A.1.8 Предел огнестойкости стен, перекрытий, лестничных клеток", CheckCategory.Norms, Fire),
            new CheckDefinition("building-class", "A.1.9 Степень огнестойкости и класс пожарной опасности здания", CheckCategory.Norms, Fire),
            new CheckDefinition("fire-doors", "A.1.10 Противопожарные двери и тамбур-шлюзы", CheckCategory.Norms, Fire),
            new CheckDefinition("zones", "A.1.11 Разделение жилья, ДОО и ритейла противопожарными преградами", CheckCategory.Norms, Fire),

            // A.2 Жилые здания
            new CheckDefinition("res-stair-step", "A.2.1 Высота и ширина ступени, уклон марша", CheckCategory.Norms, Residential),
            new CheckDefinition("res-clear-height", "A.2.2 Высота помещений и в свету на путях эвакуации", CheckCategory.Norms, Residential),

            // A.3 Общественные здания (ДОО, ритейл)
            new CheckDefinition("pub-stair-step", "A.3.1 Высота и ширина ступени, уклон марша", CheckCategory.Norms, Public),
            new CheckDefinition("pub-clear-height", "A.3.2 Высота помещений и в свету на путях эвакуации", CheckCategory.Norms, Public),

            // A.4 МГН
            new CheckDefinition("accessibility", "A.4.1 Проходы, пандусы, лифты, санузлы для МГН", CheckCategory.Norms, Mgn),

            // A.5 Стоянки
            new CheckDefinition("parking", "A.5.1 Рампы, выезды, эвакуационные выходы, проезды", CheckCategory.Norms, Parking),

            // B. Стандарты компании (список будет расширяться)
            new CheckDefinition("std-naming", "B1. Именование видов, листов, семейств и типов", CheckCategory.CompanyStandards),
            new CheckDefinition("std-parameters", "B2. Обязательные параметры проекта и общие параметры", CheckCategory.CompanyStandards),
            new CheckDefinition("std-templates", "B3. Шаблоны видов, стили линий, штриховки, текст", CheckCategory.CompanyStandards),
            new CheckDefinition("std-worksets", "B4. Рабочие наборы и структура модели", CheckCategory.CompanyStandards),
            new CheckDefinition("std-sheets", "B5. Оформление листов и штампа по стандарту компании", CheckCategory.CompanyStandards),
            new CheckDefinition("std-finishes", "B6. Варианты отделки помещений", CheckCategory.CompanyStandards),
            new CheckDefinition("std-masonry", "B7. Типы кладки и стен", CheckCategory.CompanyStandards),
            new CheckDefinition("std-windows-doors", "B8. Типы окон и дверей", CheckCategory.CompanyStandards),

            // C. Стандарты АГР (mos.ru), «Строим просто», ДГП: все в одной папке
            new CheckDefinition("doc-sheets-set", "C1. Состав листов раздела АР", CheckCategory.Documentation),
            new CheckDefinition("doc-title-block", "C2. Заполнение основной надписи", CheckCategory.Documentation),
            new CheckDefinition("doc-plan-elements", "C3. Обязательные элементы на планах (оси, отметки, размеры, номера помещений)", CheckCategory.Documentation),
            new CheckDefinition("doc-sections-facades", "C4. Разрезы и фасады: отметки, размеры, обозначения", CheckCategory.Documentation),
            new CheckDefinition("doc-explication", "C5. Экспликации и ведомости помещений, площади", CheckCategory.Documentation),
            new CheckDefinition("doc-scales-annotations", "C6. Масштабы, выноски, марки, условные обозначения", CheckCategory.Documentation),
            new CheckDefinition("doc-standards", "C7. Требования из стандартов в папке (набор наполняется из PDF)", CheckCategory.Documentation),
        };
    }
}
