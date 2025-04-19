namespace WebApp_AppService
{
    public class Publisher
    {
        public event EventHandler SomeEvent;

        public void RaiseEvent()
        {
            SomeEvent?.Invoke(this, EventArgs.Empty);
        }
    }

    public class Subscriber
    {
        private readonly byte[] _largeData = new byte[1024 * 1024]; // 1MB of data

        public void Subscribe(Publisher publisher)
        {
            publisher.SomeEvent += OnEvent; // Never unsubscribed
        }

        private void OnEvent(object sender, EventArgs e)
        {
            Console.WriteLine($"Event received, data size: {_largeData.Length}");
        }

        public static Publisher CreatePublishers()
        {
            var publisher = new Publisher();
            for (int i = 0; i < 2_100; i++)
            {
                var subscriber = new Subscriber();
                subscriber.Subscribe(publisher);
            }

            return publisher;
        }
    }
}
