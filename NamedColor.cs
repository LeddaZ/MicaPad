using System.Collections.ObjectModel;
using System.Reflection;
using Windows.UI;

namespace MicaPad
{
    public class NamedColor
    {
        public string Name { get; set; }
        public Color Color { get; set; }
        public ObservableCollection<NamedColor> Colors { get; set; }

        public NamedColor()
        {
            foreach (var color in typeof(Colors).GetRuntimeProperties())
            {
                Colors.Add(new NamedColor() { Name = color.Name, Color = (Color)color.GetValue(null) });
            }
        }

        public ObservableCollection<NamedColor> GetColors()
        {
            return Colors;
        }

    }
}
