using System.Diagnostics;
using System.Net.WebSockets;
using System.Text;

namespace ChattyApp;

public partial class BotPage : ContentPage
{
	ClientWebSocket ws = new ClientWebSocket();
	Boolean isConnected = false;

	public BotPage()
	{
        InitializeComponent();
	}

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        InitializeWebSocket();
    }
    protected override async void OnDisappearing()
    {
        base.OnDisappearing();

        if (ws.State != WebSocketState.Closed)
        {
            await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
        }
    }

    private void OnChatEntryCompleted(object sender, EventArgs e)
	{
        string text = chatentry.Text;
        if (text == "" | text == "\n")
        {
            return;
        }
        Trace.WriteLine(text);
		SendToWebSocket(text);
		chatentry.UpdateText("");
		AddUserMessageToScrollView(text);
	}

	private async void AddUserMessageToScrollView(string message)
	{
		Frame bubble = CreateBubble(message, false);
		chatwindow.Add(bubble);
        await messageScrollView.ScrollToAsync(messageScrollView, ScrollToPosition.End, false);
    }

    private async void AddBotMessageToScrollView(string message)
    {
        Frame bubble = CreateBubble(message, true);
        chatwindow.Add(bubble);
        await messageScrollView.ScrollToAsync(chatwindow, ScrollToPosition.End, false);
    }
    private async void InitializeWebSocket()
	{
		try
		{
            Trace.WriteLine("Attempting connection");
            await ws.ConnectAsync(new Uri("wss://api.bot.demo.pinguin-it.de/chat"), CancellationToken.None);
            Trace.WriteLine("Connected to socket");
            isConnected = true;

            AddBotMessageToScrollView("Hi, wie kann ich Ihnen helfen?");

            var receiveTask = Task.Run(async () =>
            {
                // TODO: Not so nice
                var buffer = new byte[1024 * 4];
                while (true)
                {
                    var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        break;
                    }

                    var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    Trace.WriteLine(message);
                    _ = MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        AddBotMessageToScrollView(message);
                    });
                }
            });

            await receiveTask;
        } catch(Exception ex)
        {
            Trace.WriteLine(ex.ToString());
            AddBotMessageToScrollView("Hi, momentan kann ich mich leider nicht mit zuhause verbinden. Bitte versuchen Sie es bitte nachher erneut");
        }
		
	}

	private Frame CreateBubble(string message, bool isBot)
	{
		Frame frame = new Frame();

		frame.CornerRadius = 10;
		frame.Padding = 10;

        Label label = new Label();

		if (isBot)
		{
			frame.HorizontalOptions = LayoutOptions.Start;
            frame.BackgroundColor = Colors.Blue;
        }
		else
		{
			frame.HorizontalOptions = LayoutOptions.End;
            frame.BackgroundColor = Colors.DarkBlue;
        }

		label.VerticalOptions = LayoutOptions.Center;

		label.Text = message;

		frame.Content = label;

		return frame;
	}

	private async void SendToWebSocket(string message)
	{
        if (ws.State == WebSocketState.Open)
        {
            var bytes = Encoding.UTF8.GetBytes(message.Replace(" ", ""));
            var arraySegment = new ArraySegment<byte>(bytes, 0, bytes.Length);
            await ws.SendAsync(arraySegment, WebSocketMessageType.Text, true, CancellationToken.None);
        } else
        {
            AddBotMessageToScrollView("Hi, momentan kann ich mich leider nicht mit zuhause verbinden. Bitte versuchen Sie es bitte nachher erneut");
        }
	}
}