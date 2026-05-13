# План разработки BarcodeGenerator (DataMatrix / GS1)

## Обзор проекта

Два приложения на C# для генерации DataMatrix и GS1-128 баркодов:
- **BarcodeGenerator.Avalonia** — десктопное GUI-приложение (Avalonia UI, MVVM)
- **BarcodeGenerator.Console** — консольное приложение с тем же функциональным ядром

---

## Структура решения

```
BarcodeGenerator.sln
├── src/
│   ├── BarcodeGenerator.Core/          # Общая библиотека (бизнес-логика)
│   ├── BarcodeGenerator.Avalonia/      # Avalonia UI приложение
│   └── BarcodeGenerator.Console/       # Консольное приложение
└── tests/
    └── BarcodeGenerator.Core.Tests/    # Юнит-тесты ядра
```

---

## Фаза 1 — BarcodeGenerator.Core (общая библиотека)

### 1.1 Зависимости (NuGet)

| Пакет | Версия | Назначение |
|---|---|---|
| `ZXing.Net` | ≥ 0.16.x | Кодирование DataMatrix / GS1 |
| `SkiaSharp` | ≥ 2.88.x | Растеризация баркода в PNG |
| `SkiaSharp.NativeAssets.Linux` / `Win32` | — | Нативные биндинги (платформозависимо) |

> **Альтернатива ZXing:** `barcodelib` или `Zen.Barcode.Core`. ZXing предпочтителен — поддерживает DataMatrix ECC200 и GS1.

### 1.2 Модели данных

```csharp
// Тип кодировки
public enum BarcodeFormat { DataMatrix, GS1DataMatrix, GS1_128 }

// Параметры одного баркода
public record BarcodeRequest(
    string Data,
    BarcodeFormat Format,
    int ModuleSize = 10,   // размер одной ячейки в px
    int Margin = 10        // отступ вокруг символа в px
);

// Результат генерации
public record BarcodeResult(
    string Data,
    SKBitmap Bitmap,       // растровое изображение
    byte[] PngBytes        // готовый PNG
);
```

### 1.3 Интерфейс и реализация генератора

```csharp
public interface IBarcodeGenerator
{
    // Сгенерировать один баркод
    BarcodeResult Generate(BarcodeRequest request);

    // Сгенерировать список баркодов
    IReadOnlyList<BarcodeResult> GenerateBatch(IEnumerable<BarcodeRequest> requests);
}
```

**Реализация `ZXingBarcodeGenerator`:**

1. Создать `BarcodeWriter<SKBitmap>` из ZXing.Net
2. Настроить `EncodingOptions`:
   - `Width` / `Height` = `ModuleSize * матрица_символа`
   - `Margin` = `request.Margin`
   - Для GS1: выставить `PureBarcode = false`, использовать `FNC1` mode
3. Вызвать `writer.Write(data)` → `SKBitmap`
4. Закодировать bitmap в PNG через `SKBitmap.Encode(SKEncodedImageFormat.Png, 100)`
5. Вернуть `BarcodeResult`

### 1.4 Сервис пакетной / серийной генерации

```csharp
public class SerialCodeService
{
    // Разбить одну строку (по переносам / разделителю) на список кодов
    public IReadOnlyList<string> ParseMultiline(string input);

    // Сгенерировать серию: baseCode + порядковый номер с ведущими нулями
    // Пример: GenerateSeries("ABC", 1, 5, 4) → "ABC0001".."ABC0005"
    public IReadOnlyList<string> GenerateSeries(
        string baseCode,
        int startIndex,
        int count,
        int digits);        // количество цифр (ведущие нули)
}
```

### 1.5 Сервис сохранения PNG

```csharp
public class PngExportService
{
    // Сохранить единственный баркод
    Task SaveSingleAsync(BarcodeResult result, string filePath);

    // Сохранить все баркоды: каждый в отдельный файл
    // именование: {baseName}_{index:D4}.png
    Task SaveBatchAsync(IReadOnlyList<BarcodeResult> results, string directory, string baseName);

    // Сохранить все баркоды в одно изображение-стрип (вертикальный лист)
    Task SaveStripAsync(IReadOnlyList<BarcodeResult> results, string filePath, int padding = 20);
}
```

**Алгоритм `SaveStripAsync`:**
1. Вычислить суммарную высоту + паддинги
2. Создать `SKBitmap` с белым фоном нужного размера
3. Нарисовать каждый `BarcodeResult.Bitmap` с вертикальным смещением
4. Под каждым баркодом (опционально) подписать `Data` через `SKCanvas.DrawText`
5. Сохранить PNG

---

## Фаза 2 — BarcodeGenerator.Avalonia

### 2.1 Зависимости (NuGet, сверх Core)

| Пакет | Назначение |
|---|---|
| `Avalonia` ≥ 11.x | UI фреймворк |
| `Avalonia.Desktop` | Десктоп-хост |
| `Avalonia.Themes.Fluent` | Тема оформления |
| `CommunityToolkit.Mvvm` | Source-gen MVVM (RelayCommand, ObservableProperty) |
| `Avalonia.Skia` | Интеграция SkiaSharp с Avalonia |

### 2.2 Архитектура (MVVM)

```
Views/
  MainWindow.axaml(.cs)
  SingleBarcodeView.axaml(.cs)
  MultiBarcodesView.axaml(.cs)
  SeriesGeneratorView.axaml(.cs)
ViewModels/
  MainWindowViewModel.cs
  SingleBarcodeViewModel.cs
  MultiBarcodesViewModel.cs
  SeriesGeneratorViewModel.cs
Controls/
  BarcodeImageControl.axaml(.cs)   # кастомный контрол отрисовки SKBitmap
Services/
  AvaloniaDialogService.cs         # обёртка SaveFileDialog
```

### 2.3 MainWindow — навигация по вкладкам

`MainWindow` содержит `TabControl` с тремя вкладками:

| # | Название вкладки | ViewModel |
|---|---|---|
| 1 | Один баркод | `SingleBarcodeViewModel` |
| 2 | Несколько баркодов | `MultiBarcodesViewModel` |
| 3 | Серия баркодов | `SeriesGeneratorViewModel` |

---

### 2.4 Вкладка 1 — «Один баркод»

**UI (`SingleBarcodeView.axaml`):**

```
┌─────────────────────────────────────────┐
│  Формат: [ComboBox: DataMatrix / GS1]   │
│  Данные: [TextBox multiline]            │
│  Размер ячейки: [NumericUpDown]         │
│  [Кнопка: Сгенерировать]               │
│ ┌─────────────────────────────────────┐ │
│ │      BarcodeImageControl            │ │
│ │      (отрисовка SKBitmap)           │ │
│ └─────────────────────────────────────┘ │
│  [Кнопка: Сохранить PNG]               │
└─────────────────────────────────────────┘
```

**`SingleBarcodeViewModel.cs`:**

```csharp
[ObservableProperty] string inputData;
[ObservableProperty] BarcodeFormat selectedFormat;
[ObservableProperty] int moduleSize = 10;
[ObservableProperty] BarcodeResult? currentResult;

[RelayCommand]
void Generate() { /* вызов IBarcodeGenerator.Generate */ }

[RelayCommand]
async Task SavePng() { /* SaveFileDialog → PngExportService.SaveSingleAsync */ }
```

**`BarcodeImageControl`:**
- Наследует `Control`
- `BitmapProperty` — `AvaloniaProperty<SKBitmap?>`
- `Render(DrawingContext context)` преобразует `SKBitmap` → `Avalonia.Media.Imaging.Bitmap` и рисует через `context.DrawImage`

---

### 2.5 Вкладка 2 — «Несколько баркодов»

**UI (`MultiBarcodesView.axaml`):**

```
┌─────────────────────────────────────────┐
│  Режим отображения: ○ Список  ○ По одному│
│  Форматы: [ComboBox]                    │
│  Данные (каждый код на новой строке):   │
│  [TextBox multiline, высота ~150px]     │
│  [Кнопка: Сгенерировать]               │
│ ─────────── Режим "Список" ────────────│
│ ┌─────────────────────────────────────┐ │
│ │  ScrollViewer                       │ │
│ │    ItemsControl (BarcodeImageControl│ │
│ │    + подпись кода под каждым)       │ │
│ └─────────────────────────────────────┘ │
│ ─────────── Режим "По одному" ─────────│
│     [◄]  [BarcodeImageControl]  [►]    │
│          Код 3 из 10: "ABC003"         │
│  [Кнопка: Сохранить текущий PNG]       │
│  [Кнопка: Сохранить все PNG]           │
│  [Кнопка: Сохранить стрип PNG]         │
└─────────────────────────────────────────┘
```

**`MultiBarcodesViewModel.cs`:**

```csharp
[ObservableProperty] string inputData;           // multiline
[ObservableProperty] bool isListMode = true;
[ObservableProperty] int currentIndex = 0;
ObservableCollection<BarcodeResult> Results;

// Навигация
[RelayCommand(CanExecute = nameof(CanGoPrev))]
void Prev() { CurrentIndex--; }

[RelayCommand(CanExecute = nameof(CanGoNext))]
void Next() { CurrentIndex++; }

bool CanGoPrev => CurrentIndex > 0;
bool CanGoNext => CurrentIndex < Results.Count - 1;

BarcodeResult? CurrentResult => Results.ElementAtOrDefault(CurrentIndex);

[RelayCommand] async Task SaveCurrent();   // текущий
[RelayCommand] async Task SaveAll();       // все в отдельные файлы
[RelayCommand] async Task SaveStrip();     // один стрип
```

---

### 2.6 Вкладка 3 — «Серия баркодов»

**UI (`SeriesGeneratorView.axaml`):**

```
┌─────────────────────────────────────────┐
│  Базовый код: [TextBox]                 │
│  Начальный номер: [NumericUpDown]       │
│  Количество: [NumericUpDown]            │
│  Значащих цифр (нули): [NumericUpDown]  │
│  Формат: [ComboBox]                     │
│  [Кнопка: Сгенерировать серию]         │
│  ──────────────────────────────────────│
│  (далее тот же UI, что во вкладке 2)   │
│  Режим: ○ Список  ○ По одному          │
│    [◄]  [BarcodeImageControl]  [►]     │
│         "BASE0001" (1 из 50)           │
│  [Сохранить текущий] [Сохранить все]   │
│  [Сохранить стрип]                     │
└─────────────────────────────────────────┘
```

**`SeriesGeneratorViewModel.cs`:**

```csharp
[ObservableProperty] string baseCode;
[ObservableProperty] int startIndex = 1;
[ObservableProperty] int count = 10;
[ObservableProperty] int digits = 4;        // количество цифр → ведущие нули
[ObservableProperty] BarcodeFormat selectedFormat;

[RelayCommand]
void GenerateSeries()
{
    var codes = _serialService.GenerateSeries(BaseCode, StartIndex, Count, Digits);
    var requests = codes.Select(c => new BarcodeRequest(c, SelectedFormat, ModuleSize));
    Results = new(_generator.GenerateBatch(requests));
    CurrentIndex = 0;
}
// + навигация и сохранение аналогично вкладке 2
```

### 2.7 Диалог сохранения файла

```csharp
// AvaloniaDialogService.cs
public class AvaloniaDialogService
{
    public async Task<string?> ShowSaveFileDialogAsync(
        string defaultName,
        string filter = "PNG Image (*.png)|*.png")
    {
        var dialog = new SaveFileDialog { InitialFileName = defaultName };
        dialog.Filters.Add(new FileDialogFilter { Name = "PNG", Extensions = { "png" } });
        return await dialog.ShowAsync(App.MainWindow);
    }

    public async Task<string?> ShowSaveFolderDialogAsync() { /* OpenFolderDialog */ }
}
```

---

## Фаза 3 — BarcodeGenerator.Console

### 3.1 Зависимости

| Пакет | Назначение |
|---|---|
| `System.CommandLine` ≥ 2.x | Парсинг аргументов CLI |
| Ссылка на `BarcodeGenerator.Core` | Бизнес-логика |

### 3.2 Команды CLI

```
barcodegen <command> [options]

Commands:
  single      Сгенерировать один баркод
  batch       Сгенерировать баркоды из файла или строки
  series      Сгенерировать числовую серию

Options (общие):
  --format    <datamatrix|gs1datamatrix|gs1_128>  [default: datamatrix]
  --module    <int>    Размер ячейки в px          [default: 10]
  --margin    <int>    Отступ в px                 [default: 10]
  --out       <path>   Выходная папка              [default: .]
```

#### Команда `single`

```
barcodegen single --data "010460043993125621SN12345" --out ./output
```

- Генерирует `output/barcode.png`

#### Команда `batch`

```
barcodegen batch --file codes.txt --out ./output
barcodegen batch --data "CODE1\nCODE2\nCODE3" --out ./output
```

- `--file` — текстовый файл, один код на строку
- `--strip` — дополнительно сохранить стрип-изображение `strip.png`
- Именование: `barcode_0001.png`, `barcode_0002.png`, …

#### Команда `series`

```
barcodegen series --base "010460043993125621SN" --start 1 --count 100 --digits 4 --out ./output
```

- Генерирует `output/barcode_0001.png` … `output/barcode_0100.png`
- `--strip` — дополнительно сохранить `strip.png`

### 3.3 Структура кода `Program.cs`

```csharp
var rootCommand = new RootCommand("DataMatrix / GS1 Barcode Generator");

// single
var singleCmd = new Command("single", "Generate single barcode");
singleCmd.AddOption(dataOption);
singleCmd.AddOption(formatOption);
singleCmd.AddOption(outOption);
singleCmd.SetHandler(async (data, format, outDir) =>
{
    var result = generator.Generate(new BarcodeRequest(data, format));
    await exportService.SaveSingleAsync(result, Path.Combine(outDir, "barcode.png"));
    Console.WriteLine($"Saved: barcode.png");
}, dataOption, formatOption, outOption);

// batch
// series
// ... аналогично

rootCommand.AddCommand(singleCmd);
rootCommand.AddCommand(batchCmd);
rootCommand.AddCommand(seriesCmd);
return await rootCommand.InvokeAsync(args);
```

---

## Фаза 4 — Тесты (BarcodeGenerator.Core.Tests)

| Тест-класс | Что проверяет |
|---|---|
| `ZXingBarcodeGeneratorTests` | Генерирует баркод, PNG-байты не пусты, ширина/высота > 0 |
| `SerialCodeServiceTests` | `ParseMultiline` разбивает строку корректно; `GenerateSeries` генерирует нужное количество кодов с правильными суффиксами |
| `PngExportServiceTests` | Файлы создаются в tempdir, strip не пустой |

Фреймворк: **xUnit** + **FluentAssertions**

---

## Фаза 5 — Сборка и публикация

### 5.1 Структура `.csproj`

**Core** — `net10.0`  
**Avalonia** — `net10.0`  
**Console** — `net10.0`

### 5.2 Publish-профили

```
# Самодостаточный exe для Windows x64
dotnet publish BarcodeGenerator.Console -r win-x64 -c Release --self-contained true -p:PublishSingleFile=true

# Avalonia Windows
dotnet publish BarcodeGenerator.Avalonia -r win-x64 -c Release --self-contained true -p:PublishSingleFile=true

# Требуется .NET 10 SDK (preview или финальный)
# https://dotnet.microsoft.com/download/dotnet/10.0
```

---

## Порядок выполнения (рекомендуемый)

```
[ ] 1.  Создать sln и три проекта (Core, Avalonia, Console)
[ ] 2.  Добавить NuGet-зависимости в каждый проект
[ ] 3.  Реализовать модели данных в Core
[ ] 4.  Реализовать ZXingBarcodeGenerator (DataMatrix + GS1)
[ ] 5.  Реализовать SerialCodeService
[ ] 6.  Реализовать PngExportService (single / batch / strip)
[ ] 7.  Написать юнит-тесты для Core
[ ] 8.  Реализовать BarcodeImageControl (Avalonia, SkiaSharp)
[ ] 9.  Реализовать SingleBarcodeView + ViewModel
[ ] 10. Реализовать MultiBarcodesView + ViewModel (список + навигация)
[ ] 11. Реализовать SeriesGeneratorView + ViewModel
[ ] 12. Собрать MainWindow с TabControl, подключить DI (вручную или через DryIoc/Microsoft.Extensions.DI)
[ ] 13. Реализовать Console: single, batch, series команды
[ ] 14. Протестировать GS1-кодирование (FNC1, AI-коды)
[ ] 15. Publish-профили, README
```

---

## Заметки по GS1

- **GS1 DataMatrix** — стандартный DataMatrix, но данные начинаются с FNC1 (символ `\x1D` или режим `GS1` в ZXing)
- **Application Identifier (AI):** `(01)` — GTIN, `(21)` — серийный номер, `(17)` — срок годности и т.д.
- Пример строки GS1: `\x1D010460043993125621SN12345`  
  или с явными AI-скобками (только для отображения): `(01)04600439931256(21)SN12345`
- ZXing.Net: использовать `BarcodeFormat.DATA_MATRIX` + `EncodingOptions.Hints[EncodeHintType.DATA_MATRIX_SHAPE] = SymbolShapeHint.FORCE_SQUARE` для квадратной матрицы

---

## Ссылки

- [ZXing.Net GitHub](https://github.com/micjahn/ZXing.Net)
- [SkiaSharp docs](https://github.com/mono/SkiaSharp)
- [Avalonia UI docs](https://docs.avaloniaui.net/)
- [GS1 General Specifications](https://www.gs1.org/standards/barcodes)
- [System.CommandLine](https://github.com/dotnet/command-line-api)
