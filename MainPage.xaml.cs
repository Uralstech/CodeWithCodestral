using CodeWithCodestral.MistralApi;
using CodeWithCodestral.ViewModels;

namespace CodeWithCodestral;

public partial class MainPage : ContentPage
{
	public MainPage(MainViewModel model)
	{
		InitializeComponent();
		BindingContext = model;
    }

    private async void Grid_Loaded(object sender, EventArgs e)
    {
		await ((MainViewModel)BindingContext).RunSetupProcedures();
	}

    public void AddMessage(MistralChatMessage message)
	{
		ChatWindow.Add(
			new StackLayout
			{
				Padding = 10,
				Spacing = 5,

				Children =
				{
					new Label
					{
						Text = message.Role.ToString(),
						FontFamily = "OpenSans",

						HorizontalOptions = message.Role == MistralChatMessageRole.User
							? LayoutOptions.End
							: LayoutOptions.Start,
					},

					new Editor
					{
						Text = message.Content,
						IsReadOnly = true,
						IsSpellCheckEnabled = false,
					}
				}
			});
    }
}