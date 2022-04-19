﻿using Octokit;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Provider;
using Windows.System.Profile;
using Windows.UI;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace MicaPad
{

    public sealed partial class MainPage : Windows.UI.Xaml.Controls.Page
    {

        private bool hasUnsavedChanges = false;
        private int editCount = 0;
        private StorageFile currentFile;

        public MainPage()
        {
            this.InitializeComponent();

            // Gets all fonts installed system-wide and adds them to the font flyout
            var fonts = Microsoft.Graphics.Canvas.Text.CanvasTextFormat.GetSystemFontFamilies();
            int i = 0;
            foreach (var item in fonts)
            {
                var flyoutItem = new MenuFlyoutItem
                {
                    Text = item
                };
                flyoutItem.Click += FontFlyoutItem_Click;
                FontFlyout.Items.Insert(i, flyoutItem);
                i++;
            }

            // Sets corner radius for all items in the font flyout
            foreach (var item in FontFlyout.Items)
            {
                item.CornerRadius = new CornerRadius(4);
            }

            // Show warning button on Windows 10
            if (GetWinVer().Equals("10"))
            {
                warningButton.Visibility = Visibility.Visible;
            }

            // Run update check on app startup
            _ = CheckUpdates();
        }

        // Returns white or black based on the given color
        private Color PickTextColor(SolidColorBrush sourceColor)
        {
            var r = sourceColor.Color.R;
            var g = sourceColor.Color.G;
            var b = sourceColor.Color.B;
            return (((r * 0.299) + (g * 0.587) + (b * 0.114)) > 145) ? Color.FromArgb(255, 0, 0, 0) : Color.FromArgb(255, 255, 255, 255);
        }

        // Sets my custom button style
        private Style SetButtonStyle(bool isDefaultButton)
        {
            var buttonStyle = new Style(typeof(Button));
            if (isDefaultButton)
            {
                var color = new UISettings().GetColorValue(UIColorType.Accent);
                var brush = new SolidColorBrush(Color.FromArgb(color.A, color.R, color.G, color.B));
                buttonStyle.Setters.Add(new Setter(BackgroundProperty, brush));
                buttonStyle.Setters.Add(new Setter(ForegroundProperty, PickTextColor(brush)));
            }
            buttonStyle.Setters.Add(new Setter(CornerRadiusProperty, new CornerRadius(4)));
            return buttonStyle;
        }

        // Returns Windows build number
        private string GetWinBuild()
        {
            string deviceFamilyVersion = AnalyticsInfo.VersionInfo.DeviceFamilyVersion;
            ulong version = ulong.Parse(deviceFamilyVersion);
            ulong build = (version & 0x00000000FFFF0000L) >> 16;
            return build.ToString();
        }

        // Returns Windows version
        private string GetWinVer()
        {
            return int.Parse(GetWinBuild()) >= 21996 ? "11" : "10";
        }

        // Returns app version
        public string GetVersion()
        {
            Package package = Package.Current;
            PackageId packageId = package.Id;
            PackageVersion version = packageId.Version;
            return string.Format("{0}.{1}.{2}", version.Major, version.Minor, version.Build);
        }

        // Checks for updates
        private async Task CheckUpdates()
        {
            GitHubClient client = new GitHubClient(new ProductHeaderValue("opnotepad"));
            IReadOnlyList<Release> releases = await client.Repository.Release.GetAll("LeddaZ", "MicaPad");

            // Setup versions
            var latestVersion = new Version(releases[0].Name);
            Version localVersion = new Version(GetVersion());

            // Compare the versions
            int versionComparison = localVersion.CompareTo(latestVersion);
            if (versionComparison < 0)
            {
                ContentDialog updateDialog = new ContentDialog
                {
                    Title = "Update available",
                    Content = $"Current version: {GetVersion()}\nLatest version: {latestVersion}",
                    PrimaryButtonText = "Update",
                    CloseButtonText = "Ignore",
                    CornerRadius = new CornerRadius(8)
                };
                updateDialog.PrimaryButtonStyle = SetButtonStyle(true);
                updateDialog.CloseButtonStyle = SetButtonStyle(false);
                ContentDialogResult result = await updateDialog.ShowAsync();

                // Go to GitHub if the user clicks the "Update" button
                if (result == ContentDialogResult.Primary)
                {
                    Uri uri = new Uri(@"https://github.com/LeddaZ/MicaPad/releases/latest");
                    _ = await Windows.System.Launcher.LaunchUriAsync(uri);
                }
            }
        }

        // Sets the chosen font size
        private void SizeFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            MenuFlyoutItem item = (MenuFlyoutItem)sender;
            editor.Document.Selection.CharacterFormat.Size = float.Parse(item.Text);
        }

        // Sets the chosen font family
        private void FontFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            MenuFlyoutItem item = (MenuFlyoutItem)sender;
            editor.Document.Selection.CharacterFormat.Name = item.Text.ToString();

        }

        // Shows the open file dialog and opens the file once one is selected
        private async void OpenButton_Click(object sender, RoutedEventArgs e)
        {
            /* Workaround to stop the RichEditBox from detecting unsaved changes even if the
             * file has just been opened */
            editCount = 0;

            /* Checks if the current document has unsaved changes and shows a confirmation
             * dialog in that case */
            if (hasUnsavedChanges)
            {
                ContentDialog unsavedChangesDialog = new ContentDialog
                {
                    Title = "MicaPad",
                    Content = "There are unsaved changes. Do you want to save the file?",
                    PrimaryButtonText = "Save",
                    SecondaryButtonText = "Don't save",
                    CloseButtonText = "Cancel",
                    CornerRadius = new CornerRadius(8)
                };
                unsavedChangesDialog.PrimaryButtonStyle = SetButtonStyle(true);
                unsavedChangesDialog.SecondaryButtonStyle = SetButtonStyle(false);
                unsavedChangesDialog.CloseButtonStyle = SetButtonStyle(false);
                ContentDialogResult result = await unsavedChangesDialog.ShowAsync();

                switch (result)
                {
                    case ContentDialogResult.Primary:
                        /* await is needed, otherwise the save and open dialogs will show
                         * at the same time */
                        try
                        {
                            await Save();
                        }
                        catch (System.IO.FileLoadException)
                        {
                            ContentDialog errorDialog = new ContentDialog()
                            {
                                Title = "File saving error",
                                Content = "Sorry, I couldn't save the file.",
                                CloseButtonText = "Ok",
                                CornerRadius = new CornerRadius(8)
                            };
                            errorDialog.CloseButtonStyle = SetButtonStyle(true);
                            await errorDialog.ShowAsync();
                        }
                        break;
                    case ContentDialogResult.Secondary:
                        break;
                    case ContentDialogResult.None:
                        return;
                }
            }

            // Opens the rtf file
            FileOpenPicker open = new FileOpenPicker
            {
                SuggestedStartLocation = PickerLocationId.DocumentsLibrary
            };
            open.FileTypeFilter.Add(".rtf");

            StorageFile file = await open.PickSingleFileAsync();

            if (file != null)
            {
                try
                {
                    Windows.Storage.Streams.IRandomAccessStream randAccStream = await file.OpenAsync(FileAccessMode.Read);

                    // Load the file into the Document property of the RichEditBox
                    editor.Document.LoadFromStream(Windows.UI.Text.TextSetOptions.FormatRtf, randAccStream);
                }
                catch (Exception)
                {
                    ContentDialog errorDialog = new ContentDialog()
                    {
                        Title = "File open error",
                        Content = "Sorry, I couldn't open the file.",
                        CloseButtonText = "Ok",
                        CornerRadius = new CornerRadius(8)
                    };
                    errorDialog.CloseButtonStyle = SetButtonStyle(true);
                    await errorDialog.ShowAsync();
                }
            }

            // Store the current file
            currentFile = file;

            // The file has just been opened, so there are no unsaved changes
            hasUnsavedChanges = false;
            editCount = 0;
        }

        // Calls the Save() method to save the file
        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await Save();
            }
            catch (System.IO.FileLoadException)
            {
                ContentDialog errorDialog = new ContentDialog()
                {
                    Title = "File saving error",
                    Content = "Sorry, I couldn't save the file.",
                    CloseButtonText = "Ok",
                    CornerRadius = new CornerRadius(8)
                };
                errorDialog.CloseButtonStyle = SetButtonStyle(true);
                await errorDialog.ShowAsync();
            }
        }

        // Saves the file
        private async Task Save()
        {
            StorageFile file = currentFile;

            if (file != null)
            {
                /* Prevent updates to the remote version of the file until we
                 * finish making changes and call CompleteUpdatesAsync */
                CachedFileManager.DeferUpdates(file);

                // write to file
                Windows.Storage.Streams.IRandomAccessStream randAccStream = await file.OpenAsync(FileAccessMode.ReadWrite);

                editor.Document.SaveToStream(Windows.UI.Text.TextGetOptions.FormatRtf, randAccStream);

                /* Let Windows know that we're finished changing the file so the
                 * other app can update the remote version of the file */
                FileUpdateStatus status = await CachedFileManager.CompleteUpdatesAsync(file);
                if (status != FileUpdateStatus.Complete)
                {
                    Windows.UI.Popups.MessageDialog errorBox = new Windows.UI.Popups.MessageDialog("File " + file.Name + " couldn't be saved.");
                    await errorBox.ShowAsync();
                }

                // Since the file has been saved, there are no unsaved changes now
                hasUnsavedChanges = false;
            }
        }

        // Shows the file save dialog and saves the file afterwards
        private async void SaveAsButton_Click(object sender, RoutedEventArgs e)
        {
            FileSavePicker savePicker = new FileSavePicker
            {
                SuggestedStartLocation = PickerLocationId.DocumentsLibrary
            };

            // Dropdown of file types the user can save the file as
            savePicker.FileTypeChoices.Add("Rich Text", new List<string>() { ".rtf" });

            // Default file name if the user does not type one in or select a file to replace
            savePicker.SuggestedFileName = "New Document";

            StorageFile file = await savePicker.PickSaveFileAsync();
            if (file != null)
            {
                // Prevent updates to the remote version of the file until we
                // finish making changes and call CompleteUpdatesAsync.
                CachedFileManager.DeferUpdates(file);
                // write to file
                using (Windows.Storage.Streams.IRandomAccessStream randAccStream =
                    await file.OpenAsync(FileAccessMode.ReadWrite))
                {
                    editor.Document.SaveToStream(Windows.UI.Text.TextGetOptions.FormatRtf, randAccStream);
                }

                // Let Windows know that we're finished changing the file so the
                // other app can update the remote version of the file.
                FileUpdateStatus status = await CachedFileManager.CompleteUpdatesAsync(file);
                if (status != FileUpdateStatus.Complete)
                {
                    Windows.UI.Popups.MessageDialog errorBox =
                        new Windows.UI.Popups.MessageDialog("File " + file.Name + " couldn't be saved.");
                    await errorBox.ShowAsync();
                }
            }

            // Store the current file
            currentFile = file;

            // Since the file has been saved, there are no unsaved changes now
            hasUnsavedChanges = false;
        }

        // Shows the about dialog
        private async void AboutButton_Click(object sender, RoutedEventArgs e)
        {
            ContentDialog aboutDialog = new ContentDialog()
            {
                Title = "About MicaPad",
                Content = $"MicaPad is a Notepad alternative with some additional features, like Rich Text support and the beautiful Mica backdrop (which inspired the name).\nVersion {GetVersion()}, running on Windows {GetWinVer()} build {GetWinBuild()}.",
                CloseButtonText = "Close",
                CornerRadius = new CornerRadius(8)
            };
            aboutDialog.CloseButtonStyle = SetButtonStyle(true);
            _ = await aboutDialog.ShowAsync();
        }

        // Shows the warning dialog
        private async void WarningButton_Click(object sender, RoutedEventArgs e)
        {
            ContentDialog warningDialog = new ContentDialog()
            {
                Title = "Warning",
                Content = "Since you are using Windows 10, you won't see the Mica backdrop, as it's only supported on Windows 11.",
                CloseButtonText = "Close",
                CornerRadius = new CornerRadius(8)
            };
            warningDialog.CloseButtonStyle = SetButtonStyle(true);
            _ = await warningDialog.ShowAsync();
        }

        // Enables or disables spell check
        private void SpellCheckButton_Click(object sender, RoutedEventArgs e)
        {
            if(editor.IsSpellCheckEnabled)
            {
                editor.IsSpellCheckEnabled = false;
            }
            else
            {
                editor.IsSpellCheckEnabled = true;
                
            }
        }

        // Enables or disables bold text
        private void BoldButton_Click(object sender, RoutedEventArgs e)
        {
            Windows.UI.Text.ITextSelection selectedText = editor.Document.Selection;
            if (selectedText != null)
            {
                Windows.UI.Text.ITextCharacterFormat charFormatting = selectedText.CharacterFormat;
                charFormatting.Bold = Windows.UI.Text.FormatEffect.Toggle;
                selectedText.CharacterFormat = charFormatting;
            }
        }

        // Enables or disables italic text
        private void ItalicButton_Click(object sender, RoutedEventArgs e)
        {
            Windows.UI.Text.ITextSelection selectedText = editor.Document.Selection;
            if (selectedText != null)
            {
                Windows.UI.Text.ITextCharacterFormat charFormatting = selectedText.CharacterFormat;
                charFormatting.Italic = Windows.UI.Text.FormatEffect.Toggle;
                selectedText.CharacterFormat = charFormatting;
            }
        }

        // Enables or disables underlined text
        private void UnderlineButton_Click(object sender, RoutedEventArgs e)
        {
            Windows.UI.Text.ITextSelection selectedText = editor.Document.Selection;
            if (selectedText != null)
            {
                Windows.UI.Text.ITextCharacterFormat charFormatting = selectedText.CharacterFormat;
                if (charFormatting.Underline == Windows.UI.Text.UnderlineType.None)
                {
                    charFormatting.Underline = Windows.UI.Text.UnderlineType.Single;
                }
                else
                {
                    charFormatting.Underline = Windows.UI.Text.UnderlineType.None;
                }
                selectedText.CharacterFormat = charFormatting;
            }

        }

        // Sets the font color
        private void ColorButton_Click(object sender, RoutedEventArgs e)
        {
            Button clickedColor = (Button)sender;
            var ellipse = (Windows.UI.Xaml.Shapes.Ellipse)clickedColor.Content;
            var color = ((SolidColorBrush)ellipse.Fill).Color;

            editor.Document.Selection.CharacterFormat.ForegroundColor = color;

            fontColorButton.Flyout.Hide();
            editor.Focus(FocusState.Keyboard);
        }

        // Detects when the document has been modified (it will now have unsaved changes)
        private void Editor_TextChanged(object sender, RoutedEventArgs e)
        {
            if (editCount > 0)
            {
                hasUnsavedChanges = true;
            }
            editCount++;
        }

    }

}
