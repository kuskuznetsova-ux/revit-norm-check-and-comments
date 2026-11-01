# revit-norm-check-and-comments
check layouts meet with building codes

## Плагин (Revit 2022)

Каркас: кнопка «Проверка норм» на вкладке «Надстройки», окно с выбором проверок
(список в `docs/RULES.md`). Три группы проверок: A нормы, B стандарты компании, C состав и оформление АР стадии П; для каждой своя папка с PDF. Пути и выбор запоминаются в
`%AppData%\RevitNormCheck\settings.json`. Сами проверки и выгрузка в Excel пока не реализованы.

Сборка (нужны .NET SDK и установленный Revit 2022):

```
dotnet build src/RevitNormCheck/RevitNormCheck.csproj -c Release
```

Если Revit стоит не в `C:\Program Files\Autodesk\Revit 2022`, добавьте
`-p:RevitInstallDir="D:\путь\к\Revit 2022"`.

Установка: положите `RevitNormCheck.addin` в `%AppData%\Autodesk\Revit\Addins\2022\`,
а содержимое `bin\Release\net48\` в папку `RevitNormCheck` рядом с ним.
