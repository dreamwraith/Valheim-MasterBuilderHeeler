namespace MasterBuilderHeeler.Utilities
{
    public static class HudMessageUtils
    {
        public static void ShowTopLeft(string message)
        {
            if (MessageHud.instance)
            {
                MessageHud.instance.ShowMessage(MessageHud.MessageType.TopLeft, message);
            }
        }

        public static void ShowCenter(string message)
        {
            if (MessageHud.instance)
            {
                MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, message);
            }
        }
    }
}
