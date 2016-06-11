using Module.Shared;
using Framework.WPF.Extensions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Module.HeroVirtualTabletop.Crowds;
using Module.HeroVirtualTabletop.OptionGroups;
using System.Reflection;
using Module.HeroVirtualTabletop.AnimatedAbilities;
using Module.HeroVirtualTabletop.Library.Enumerations;

namespace Module.HeroVirtualTabletop.Library.Utility
{
    public class Helper
    {
        #region Global Clipboard
        public static object GlobalClipboardObject
        {
            get;
            set;
        }

        public static object GlobalClipboardObjectParent
        {
            get;
            set;
        }

        public static ClipboardAction GlobalClipboardAction
        {
            get;
            set;
        }
        #endregion

        #region Resource Dictionary and Style related
        public static System.Windows.Style GetCustomStyle(string styleName)
        {
            System.Windows.ResourceDictionary resource = new System.Windows.ResourceDictionary
            {
                Source = new Uri(Constants.RESOURCE_DICTIONARY_PATH, UriKind.RelativeOrAbsolute)
            };
            return (System.Windows.Style)resource[styleName];
        }
        
        public static System.Windows.Style GetCustomWindowStyle()
        {
            return GetCustomStyle(Constants.CUSTOM_MODELESS_TRANSPARENT_WINDOW_STYLENAME);
        }

        #endregion

        #region JSON Serialize/Deserialize
        public static T GetDeserializedJSONFromFile<T>(string fileName)
        {
            T obj = default(T);
            if (!File.Exists(fileName))
            {
                CreateFile(fileName);
                return obj;
            }
            JsonSerializer serializer = new JsonSerializer();
            using (StreamReader sr = new StreamReader(fileName))
            using (JsonReader reader = new JsonTextReader(sr))
            {
                
                serializer.PreserveReferencesHandling = PreserveReferencesHandling.Objects;
                serializer.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
                serializer.Formatting = Formatting.Indented;
                serializer.TypeNameHandling = TypeNameHandling.Objects;

                obj = serializer.Deserialize<T>(reader);
            }
            return obj;   
        }

        public static void SerializeObjectAsJSONToFile<T>(string fileName, T obj)
        {
            try
            {
                JsonSerializer serializer = new JsonSerializer();
                using (StreamWriter sw = new StreamWriter(fileName))
                using (JsonWriter writer = new JsonTextWriter(sw))
                {

                    serializer.PreserveReferencesHandling = PreserveReferencesHandling.Objects;
                    serializer.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
                    serializer.Formatting = Formatting.Indented;
                    serializer.TypeNameHandling = TypeNameHandling.Objects;
                    serializer.Serialize(writer, obj);
                }
            }
            catch (Exception)
            {
                
            }
        }

        #endregion

        #region File I/O
        public static void CreateFile(string fileName)
        {
            FileStream fs = File.Create(fileName);
            fs.Dispose();
        }

        #endregion

        #region TreeView

        public static object GetCurrentSelectedCrowdInCrowdCollection(Object tv, out ICrowdMemberModel crowdMember)
        {
            CrowdModel containingCrowdModel = null;
            crowdMember = null;
            TreeView treeView = tv as TreeView;

            if (treeView != null && treeView.SelectedItem != null)
            {
                if (treeView.SelectedItem is CrowdModel)
                {
                    containingCrowdModel = treeView.SelectedItem as CrowdModel;
                }
                else
                {
                    DependencyObject dObject = treeView.GetItemFromSelectedObject(treeView.SelectedItem);
                    TreeViewItem tvi = dObject as TreeViewItem; // got the selected treeviewitem
                    crowdMember = tvi.DataContext as ICrowdMemberModel;
                    dObject = VisualTreeHelper.GetParent(tvi); // got the immediate parent
                    tvi = dObject as TreeViewItem; // now get first treeview item parent
                    while (tvi == null)
                    {
                        dObject = VisualTreeHelper.GetParent(dObject);
                        tvi = dObject as TreeViewItem;
                    }
                    containingCrowdModel = tvi.DataContext as CrowdModel;
                }
            }

            return containingCrowdModel;
        }

        public static object GetCurrentSelectedAnimationInAnimationCollection(Object tv, out IAnimationElement animationElement)
        {
            IAnimationElement selectedAnimationElement = null;
            animationElement = null;
            TreeView treeView = tv as TreeView;

            if (treeView != null && treeView.SelectedItem != null)
            {
                DependencyObject dObject = treeView.GetItemFromSelectedObject(treeView.SelectedItem);
                TreeViewItem tvi = dObject as TreeViewItem; // got the selected treeviewitem
                if(tvi != null)
                    selectedAnimationElement = tvi.DataContext as IAnimationElement;
                dObject = VisualTreeHelper.GetParent(tvi); // got the immediate parent
                tvi = dObject as TreeViewItem; // now get first treeview item parent
                while (tvi == null)
                {
                    dObject = VisualTreeHelper.GetParent(dObject);
                    tvi = dObject as TreeViewItem;
                    if (tvi == null)
                    {
                        var tView = dObject as TreeView;
                        if (tView != null)
                            break;
                    }
                    else
                        animationElement = tvi.DataContext as IAnimationElement;
                }
            }

            return selectedAnimationElement;
        }

        public static string GetTextFromControlObject(object control)
        {
            string text = null;
            PropertyInfo propertyInfo = control.GetType().GetProperty("Text");
            if (propertyInfo != null)
            {
                text = propertyInfo.GetValue(control).ToString();
            }
            return text;
        }

        #endregion

        #region ListBox

        public static object GetCurrentSelectedOptionInOptionGroup(Object lb)
        {
            ICharacterOption option = null;
            ListBox listBox = lb as ListBox;

            if (listBox != null && listBox.SelectedItem != null)
            {
                if (listBox.SelectedItem is ICharacterOption)
                {
                    option = listBox.SelectedItem as ICharacterOption;
                }
            }

            return option;
        }

        #endregion

        #region General Control

        public static Visual GetDescendantByType(Visual element, Type type)
        {
            if (element == null) return null;
            if (element.GetType() == type) return element;
            Visual foundElement = null;
            if (element is FrameworkElement)
                (element as FrameworkElement).ApplyTemplate();
            for (int i = 0;
                i < VisualTreeHelper.GetChildrenCount(element); i++)
            {
                Visual visual = VisualTreeHelper.GetChild(element, i) as Visual;
                foundElement = GetDescendantByType(visual, type);
                if (foundElement != null)
                    break;
            }
            return foundElement;
        }

        public static string GetContainerWindowName(object element)
        {
            Window win = null;
            string winName = "";
            
            if (element is Window)
            {
                win = element as Window;
                winName = win.Name;
            }
            else
            {
                DependencyObject dObj = element as DependencyObject;
                while (win == null)
                {
                    FrameworkElement elem = dObj as FrameworkElement;
                    dObj = elem.Parent;
                    if (dObj is Window)
                    {
                        win = dObj as Window;
                        winName = win.Name;
                        break;
                    }
                }
            }
                        
            return winName;
        }

        #endregion
    }
}
