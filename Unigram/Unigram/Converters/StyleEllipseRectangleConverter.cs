using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Api.TL;
using Unigram.ViewModels.Users;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace Unigram.Converters
{
    public class StyleEllipseRectangleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            ResourceDictionary resourceDictionary = new ResourceDictionary();
            resourceDictionary.Source = new Uri("ms-appx:///Themes/Generic.xaml", UriKind.RelativeOrAbsolute);
            bool isEllipse = false;

            if (value is TLUser || value is UserDetailsViewModel)
            {
                if (isEllipse)
                {
                    return resourceDictionary["EllipseProfilePictureView"] as Style;
                }
                return resourceDictionary["RectangleProfilePictureView"] as Style;
            }
            else
            {
                return null;
            }

        }
        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
