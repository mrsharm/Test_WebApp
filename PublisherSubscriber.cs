namespace WebApp_AppService
{
    public class Publisher
    {
        public event EventHandler? SomeEvent;

        public void RaiseEvent()
        {
            SomeEvent?.Invoke(this, EventArgs.Empty);
        }
    }

    public class Subscriber : IDisposable
    {
        private readonly byte[] _largeData = new byte[1024 * 1024]; // 1MB of data
        private Publisher? _publisher;

        public void Subscribe(Publisher publisher)
        {
            _publisher = publisher;
            publisher.SomeEvent += OnEvent;
        }

        public void Unsubscribe()
        {
            if (_publisher != null)
            {
                _publisher.SomeEvent -= OnEvent;
                _publisher = null;
            }
        }

        private void OnEvent(object? sender, EventArgs e)
        {
            Console.WriteLine($"Event received, data size: {_largeData.Length}");
        }

        public void Dispose()
        {
            Unsubscribe();
        }

        public static Publisher CreatePublishers()
        {
            var publisher = new Publisher();
            var subscribers = new List<Subscriber>();
            
            for (int i = 0; i < 2_100; i++)
            {
                var subscriber = new Subscriber();
                subscriber.Subscribe(publisher);
                subscribers.Add(subscriber);
            }

            // Clean up subscribers to prevent memory leak
            foreach (var subscriber in subscribers)
            {
                subscriber.Dispose();
            }

            return publisher;
        }
    }
}
