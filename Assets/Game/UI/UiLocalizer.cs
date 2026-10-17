using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Game.UI
{
    /// <summary>
    /// Static texts in UXML are written as "#key" (e.g. text="#ui.hud.save"). The localizer remembers
    /// the keys of those elements and fills in the text of the current language on every switch.
    /// </summary>
    public sealed class UiLocalizer
    {
        public const char KeyPrefix = '#';

        private readonly List<KeyValuePair<TextElement, string>> _items = new List<KeyValuePair<TextElement, string>>();

        /// <summary>Collects all "#key" texts under the root. Call once after the UXML is loaded.</summary>
        public void Register(VisualElement root)
        {
            root.Query<TextElement>().ForEach(element =>
            {
                string text = element.text;
                if (!string.IsNullOrEmpty(text) && text[0] == KeyPrefix)
                    _items.Add(new KeyValuePair<TextElement, string>(element, text.Substring(1)));
            });
            Apply();
        }

        public void Apply()
        {
            foreach (var item in _items) item.Key.text = Loc.Get(item.Value);
        }
    }
}
