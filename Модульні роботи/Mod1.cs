using System;
using System.Collections.Generic;
using System.Text;

namespace DeliveryServiceApp
{
    public interface IPaymentMethod
    {
        string MethodName { get; }
        void Pay(double amount);
    }

    public class CardPayment : IPaymentMethod
    {
        public string MethodName => "Картка";
        public void Pay(double amount)
        {
            Console.WriteLine($"[Оплата] Списано {amount:F2} грн з банківської картки.");
        }
    }

    public class CashPayment : IPaymentMethod
    {
        public string MethodName => "Готівка";
        public void Pay(double amount)
        {
            Console.WriteLine($"[Оплата] Отримано {amount:F2} грн готівкою кур'єру.");
        }
    }

    public class Order
    {
        public int Id { get; }
        public string Description { get; }
        public double Weight { get; }
        public double Distance { get; }

        public Transport AssignedTransport { get; set; }
        public bool IsDelivered { get; set; }
        public double FinalCost { get; set; }
        public string PaymentMethodUsed { get; set; }

        public Order(int id, string description, double weight, double distance)
        {
            Id = id;
            Description = description;
            Weight = weight;
            Distance = distance;
        }

        public override string ToString()
        {
            return $"Замовлення #{Id} (\"{Description}\", вага: {Weight} кг, відстань: {Distance} км)";
        }
    }

    public abstract class Transport
    {
        public string Name { get; }
        public double MaxWeight { get; }
        public bool IsBusy { get; set; }

        protected Transport(string name, double maxWeight)
        {
            Name = name;
            MaxWeight = maxWeight;
            IsBusy = false;
        }

        public virtual bool CanTake(Order order)
        {
            return order.Weight <= MaxWeight;
        }

        public abstract double CalculateCost(Order order);

        public override string ToString()
        {
            string status = IsBusy ? "Зайнятий" : "Вільний";
            return $"{Name} (Макс. вага: {MaxWeight} кг, Статус: {status})";
        }
    }

    public class Bike : Transport
    {
        public double MaxDistance { get; } = 10.0;

        public Bike(string name, double maxWeight) : base(name, maxWeight) { }

        public override bool CanTake(Order order)
        {
            return base.CanTake(order) && order.Distance <= MaxDistance;
        }

        public override double CalculateCost(Order order)
        {
            return 20.0 + (5.0 * order.Distance);
        }
    }

    public class Car : Transport
    {
        public Car(string name, double maxWeight) : base(name, maxWeight) { }

        public override double CalculateCost(Order order)
        {
            return 50.0 + (8.0 * order.Distance) + (1.0 * order.Weight);
        }
    }

    public class Truck : Transport
    {
        public Truck(string name, double maxWeight) : base(name, maxWeight) { }

        public override double CalculateCost(Order order)
        {
            return 200.0 + (15.0 * order.Distance) + (0.5 * order.Weight);
        }
    }

    public class DeliveryService
    {
        private readonly List<Transport> _fleet = new List<Transport>();
        private readonly List<Order> _activeOrders = new List<Order>();
        private readonly List<Order> _historyOrders = new List<Order>();

        public void AddTransport(Transport transport)
        {
            _fleet.Add(transport);
            Console.WriteLine($"[+] Додано транспорт: {transport}");
        }

        public void ShowAvailableTransportsAndCosts(Order order)
        {
            Console.WriteLine($"\n--- Розрахунок вартості для: {order} ---");
            bool anyFound = false;

            foreach (var transport in _fleet)
            {
                if (transport.CanTake(order))
                {
                    double cost = transport.CalculateCost(order);
                    string availability = transport.IsBusy ? "[ЗАРАЗ ЗАЙНЯТИЙ]" : "[ВІЛЬНИЙ]";
                    Console.WriteLine($" -> {transport.Name} {availability}: {cost:F2} грн");
                    anyFound = true;
                }
                else
                {
                    Console.WriteLine($" -> {transport.Name}: НЕ ПІДХОДИТЬ за габаритами/відстанню.");
                }
            }

            if (!anyFound)
            {
                Console.WriteLine("Жоден транспорт у парку не може взяти це замовлення за параметрами.");
            }
        }

        public bool AssignOrder(Order order, Transport transport)
        {
            Console.WriteLine($"\n[Призначення] Спроба призначити Замовлення #{order.Id} на \"{transport.Name}\"...");

            if (_activeOrders.Contains(order) || order.IsDelivered)
            {
                Console.WriteLine(" [Помилка] Це замовлення вже в роботі або вже доставлене!");
                return false;
            }

            if (transport.IsBusy)
            {
                Console.WriteLine($" [Відмова] Транспорт \"{transport.Name}\" зараз зайнятий іншим замовленням!");
                return false;
            }

            if (!transport.CanTake(order))
            {
                Console.WriteLine($" [Відмова] Транспорт \"{transport.Name}\" не проходить за обмеженнями (вага або відстань)!");
                return false;
            }

            transport.IsBusy = true;
            order.AssignedTransport = transport;
            order.FinalCost = transport.CalculateCost(order);
            _activeOrders.Add(order);

            Console.WriteLine($" [Успіх] Замовлення #{order.Id} призначено на \"{transport.Name}\". До сплати: {order.FinalCost:F2} грн.");
            return true;
        }

        public bool DeliverOrder(Order order, IPaymentMethod paymentMethod)
        {
            Console.WriteLine($"\n[Доставка] Спроба завершити доставку Замовлення #{order.Id}...");

            if (!_activeOrders.Contains(order))
            {
                Console.WriteLine($" [Помилка] Замовлення #{order.Id} не знайдено серед активних!");
                return false;
            }

            paymentMethod.Pay(order.FinalCost);

            Transport transport = order.AssignedTransport;
            transport.IsBusy = false;

            order.IsDelivered = true;
            order.PaymentMethodUsed = paymentMethod.MethodName;
            _activeOrders.Remove(order);
            _historyOrders.Add(order);

            Console.WriteLine($" [Успіх] Замовлення #{order.Id} доставлено! Транспорт \"{transport.Name}\" знову вільний.");
            return true;
        }

        public void PrintStatus()
        {
            Console.WriteLine("\n=======================================================");
            Console.WriteLine("                 СТАН СЛУЖБИ ДОСТАВКИ                  ");
            Console.WriteLine("=======================================================");

            Console.WriteLine("АКТИВНІ ЗАМОВЛЕННЯ:");
            if (_activeOrders.Count == 0)
            {
                Console.WriteLine(" (немає активних замовлень)");
            }
            else
            {
                foreach (var o in _activeOrders)
                {
                    Console.WriteLine($" * {o} | Виконує: {o.AssignedTransport.Name} | Вартість: {o.FinalCost:F2} грн");
                }
            }

            Console.WriteLine("\nІСТОРІЯ ДОСТАВОК:");
            if (_historyOrders.Count == 0)
            {
                Console.WriteLine(" (історія порожня)");
            }
            else
            {
                foreach (var o in _historyOrders)
                {
                    Console.WriteLine($" * {o} | Доставив: {o.AssignedTransport.Name} | Сплачено: {o.FinalCost:F2} грн ({o.PaymentMethodUsed})");
                }
            }

            Console.WriteLine("\nСТАН АВТОПАРКУ:");
            foreach (var t in _fleet)
            {
                Console.WriteLine($" - {t}");
            }
            Console.WriteLine("=======================================================\n");
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;

            DeliveryService service = new DeliveryService();

            Console.WriteLine("=== КРОК 1: СТВОРЕННЯ ТРАНСПОРТУ ТА ЗАМОВЛЕНЬ ===");
            Transport bike = new Bike("Велосипед Glovo (до 15 кг, до 10 км)", 15);
            Transport car = new Car("Авто Renault Kangoo (до 200 кг)", 200);
            Transport truck = new Truck("Вантажівка MAN (до 3000 кг)", 3000);

            service.AddTransport(bike);
            service.AddTransport(car);
            service.AddTransport(truck);

            Order order1 = new Order(101, "Піца та документи", 3, 5);
            Order order2 = new Order(102, "Ноутбук у передмістя", 4, 18);
            Order order3 = new Order(103, "Мішки з борошном", 80, 8);
            Order order4 = new Order(104, "Промислова піч", 850, 40);
            Order order5 = new Order(105, "Морський контейнер", 5000, 100);

            Console.WriteLine("\n=== КРОК 2: ПЕРЕВІРКА ВАРТОСТІ ДОСТАВКИ ДЛЯ ЗАМОВЛЕНЬ ===");
            service.ShowAvailableTransportsAndCosts(order1);
            service.ShowAvailableTransportsAndCosts(order2);
            service.ShowAvailableTransportsAndCosts(order3);
            service.ShowAvailableTransportsAndCosts(order4);
            service.ShowAvailableTransportsAndCosts(order5);

            Console.WriteLine("\n=== КРОК 3: ПРИЗНАЧЕННЯ ЗАМОВЛЕНЬ ===");
            service.AssignOrder(order1, bike);
            service.AssignOrder(order2, bike);
            service.AssignOrder(order4, car);
            service.AssignOrder(order2, car);
            service.AssignOrder(order4, truck);

            service.PrintStatus();

            Console.WriteLine("=== КРОК 4: ДОСТАВКА ТА ОПЛАТА ===");
            IPaymentMethod card = new CardPayment();
            IPaymentMethod cash = new CashPayment();

            service.DeliverOrder(order1, card);
            service.DeliverOrder(order2, cash);

            Order orderDistanceTest = new Order(106, "Книга в сусіднє місто", 1, 15);
            service.AssignOrder(orderDistanceTest, bike);

            service.AssignOrder(order3, car);

            Console.WriteLine("\n=== КРОК 5: ФІНАЛЬНИЙ СТАН СИСТЕМИ ===");
            service.PrintStatus();
        }
    }
}