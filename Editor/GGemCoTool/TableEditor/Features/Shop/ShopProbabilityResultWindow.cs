using System.Collections.Generic;
using System.Globalization;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace GGemCo2DCoreEditor
{
    internal sealed class ShopProbabilityResultWindow : EditorWindow
    {
        private const int DefaultIterations = 100000;

        private TableEditorDocument _document;
        private IntegerField _iterationsField;
        private ScrollView _resultScroll;
        private List<ShopProbabilityResult> _results = new List<ShopProbabilityResult>();

        public static void Open(TableEditorDocument document)
        {
            var window = GetWindow<ShopProbabilityResultWindow>();
            window.titleContent = new GUIContent("Shop Rates");
            window.minSize = new Vector2(880f, 540f);
            window._document = document;
            window.Rebuild();
            window.Show();
        }

        private void CreateGUI()
        {
            Rebuild();
        }

        private void Rebuild()
        {
            if (rootVisualElement == null)
                return;

            rootVisualElement.Clear();
            rootVisualElement.style.flexDirection = FlexDirection.Column;
            rootVisualElement.style.flexGrow = 1f;
            rootVisualElement.style.paddingLeft = 8f;
            rootVisualElement.style.paddingRight = 8f;
            rootVisualElement.style.paddingTop = 8f;
            rootVisualElement.style.paddingBottom = 8f;

            var toolbar = new Toolbar();
            _iterationsField = new IntegerField("Iterations")
            {
                value = DefaultIterations,
                style = { width = 180f }
            };
            toolbar.Add(_iterationsField);
            toolbar.Add(new Button(Calculate) { text = "Calculate" });
            toolbar.Add(new Button(CopyTsv) { text = "Copy TSV" });
            rootVisualElement.Add(toolbar);

            _resultScroll = new ScrollView
            {
                style =
                {
                    flexGrow = 1f,
                    marginTop = 8f,
                }
            };
            rootVisualElement.Add(_resultScroll);

            Calculate();
        }

        private void Calculate()
        {
            _resultScroll?.Clear();
            if (_document == null)
            {
                _resultScroll?.Add(new Label("No shop table document is loaded."));
                return;
            }

            int iterations = Mathf.Max(1, _iterationsField?.value ?? DefaultIterations);
            _results = ShopProbabilityCalculator.Calculate(_document, iterations);
            BuildResultTable(iterations);
        }

        private void CopyTsv()
        {
            GUIUtility.systemCopyBuffer = ShopProbabilityCalculator.BuildTsv(_results);
            ShowNotification(new GUIContent("Copied TSV"));
        }

        private void BuildResultTable(int iterations)
        {
            _resultScroll.Clear();
            _resultScroll.Add(new Label($"Iterations: {iterations.ToString(CultureInfo.InvariantCulture)}")
            {
                style =
                {
                    unityFontStyleAndWeight = FontStyle.Bold,
                    marginBottom = 6f,
                }
            });

            VisualElement header = CreateRow(true);
            header.Add(CreateCell("ShopUid", 70, true));
            header.Add(CreateCell("SlotIndex", 70, true));
            header.Add(CreateCell("ItemUid", 100, true));
            header.Add(CreateCell("Rate", 70, true));
            header.Add(CreateCell("UniqueGroup", 100, true));
            header.Add(CreateCell("Base %", 90, true));
            header.Add(CreateCell("Estimated %", 110, true));
            _resultScroll.Add(header);

            foreach (ShopProbabilityResult result in _results)
            {
                VisualElement row = CreateRow(false);
                row.Add(CreateCell(result.ShopUid.ToString(CultureInfo.InvariantCulture), 70));
                row.Add(CreateCell(result.SlotIndex.ToString(CultureInfo.InvariantCulture), 70));
                row.Add(CreateCell(result.ItemUid <= 0 ? "Empty" : result.ItemUid.ToString(CultureInfo.InvariantCulture), 100));
                row.Add(CreateCell(result.Rate.ToString(CultureInfo.InvariantCulture), 70));
                row.Add(CreateCell(result.UniqueGroup.ToString(CultureInfo.InvariantCulture), 100));
                row.Add(CreateCell(ShopProbabilityCalculator.FormatPercent(result.BaseProbability), 90));
                row.Add(CreateCell(ShopProbabilityCalculator.FormatPercent(result.EstimatedProbability), 110));
                _resultScroll.Add(row);
            }
        }

        private static VisualElement CreateRow(bool isHeader)
        {
            var row = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    minHeight = 22f,
                    backgroundColor = isHeader ? new Color(0.18f, 0.18f, 0.18f) : Color.clear,
                }
            };
            return row;
        }

        private static Label CreateCell(string text, int width, bool isHeader = false)
        {
            var label = new Label(text)
            {
                style =
                {
                    width = width,
                    minWidth = width,
                    paddingLeft = 4f,
                    paddingRight = 4f,
                    unityTextAlign = TextAnchor.MiddleLeft,
                    unityFontStyleAndWeight = isHeader ? FontStyle.Bold : FontStyle.Normal,
                }
            };
            return label;
        }
    }
}
