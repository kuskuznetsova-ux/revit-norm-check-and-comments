using System.Collections.Generic;

namespace RevitNormCheck.Checks
{
    public class CheckDefinition
    {
        public string Id { get; }
        public string Title { get; }

        public CheckDefinition(string id, string title)
        {
            Id = id;
            Title = title;
        }

        // Совпадает с нумерацией в docs/RULES.md.
        public static IReadOnlyList<CheckDefinition> All { get; } = new List<CheckDefinition>
        {
            new CheckDefinition("door-width", "1. Ширина эвакуационных дверей в свету"),
            new CheckDefinition("corridor-width", "2. Ширина коридоров на путях эвакуации"),
            new CheckDefinition("stair-width", "3. Ширина маршей и площадок лестниц"),
            new CheckDefinition("stair-step", "4. Высота и ширина ступени, уклон марша"),
            new CheckDefinition("door-swing", "5. Открывание эвакуационных дверей по направлению эвакуации"),
            new CheckDefinition("exits-count", "6. Число эвакуационных выходов"),
            new CheckDefinition("escape-length", "7. Длина пути эвакуации"),
            new CheckDefinition("dead-end", "8. Тупиковые коридоры"),
            new CheckDefinition("fire-rating", "9. Предел огнестойкости стен, перекрытий, лестничных клеток"),
            new CheckDefinition("building-class", "10. Степень огнестойкости и класс пожарной опасности здания"),
            new CheckDefinition("fire-doors", "11. Противопожарные двери и тамбур-шлюзы"),
            new CheckDefinition("clear-height", "12. Высота в свету на путях эвакуации"),
            new CheckDefinition("accessibility", "13. Доступность МГН"),
            new CheckDefinition("parking", "14. Паркинг: рампы, выезды, выходы"),
            new CheckDefinition("zones", "15. Разделение жилья, ДОО и ритейла"),
        };
    }
}
