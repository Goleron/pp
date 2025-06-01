using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WpfApp2;

namespace WpfApp
{
    public partial class MainWindow : Window
    {
        private ZADANIEEntities _context;
        private DateTime _lastUpdate;

        public MainWindow()
        {
            InitializeComponent();
            _context = new ZADANIEEntities();
            _lastUpdate = DateTime.Now;
            LoadData();
        }

        public DateTime LastUpdate
        {
            get { return _lastUpdate; }
            set
            {
                _lastUpdate = value;
            }
        }

        private void LoadData()
        {
            _context?.Dispose();
            _context = new ZADANIEEntities();

            var orders = _context.Orders.ToList();
            OrdersGrid.ItemsSource = orders;

            var receipts = _context.MoneyReceipts.ToList();
            ReceiptsGrid.ItemsSource = receipts;

            var payments = _context.Payments.ToList();
            var paymentsView = payments.Select(p => new PaymentView
            {
                PaymentID = p.PaymentID,
                OrderDescription = $"Заказ от {p.Orders.OrderDate:dd.MM.yyyy} на сумму {p.Orders.TotalAmount:F2}",
                ReceiptDescription = $"Поступление от {p.MoneyReceipts.ReceiptDate:dd.MM.yyyy} на сумму {p.MoneyReceipts.Amount:F2}",
                PaymentAmount = p.PaymentAmount
            }).ToList();
            PaymentsGrid.ItemsSource = paymentsView;

            OrderCombo.ItemsSource = orders.Select(o => new
            {
                o.OrderNumber,
                Display = $"Заказ {o.OrderNumber} от {o.OrderDate:dd.MM.yyyy} на сумму {o.TotalAmount:F2}"
            }).ToList();
            OrderCombo.DisplayMemberPath = "Display";
            OrderCombo.SelectedValuePath = "OrderNumber";

            ReceiptCombo.ItemsSource = receipts.Select(r => new
            {
                r.ReceiptNumber,
                Display = $"Поступление {r.ReceiptNumber} от {r.ReceiptDate:dd.MM.yyyy} на сумму {r.Amount:F2}"
            }).ToList();
            ReceiptCombo.DisplayMemberPath = "Display";
            ReceiptCombo.SelectedValuePath = "ReceiptNumber";

            _lastUpdate = DateTime.Now;
        }

        private void OrderCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ValidateForm();
        }

        private void ReceiptCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ValidateForm();
        }

        private void AmountTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !IsValidDecimalInput(e.Text, AmountTextBox.Text);
        }

        private bool IsValidDecimalInput(string input, string currentText)
        {
            if (string.IsNullOrWhiteSpace(input) && string.IsNullOrWhiteSpace(currentText)) return false;
            string newText = currentText + input;
            if (newText == ".") newText = "0.";
            if (newText.StartsWith(".")) newText = "0" + newText;
            return decimal.TryParse(newText, out decimal result) && result >= 0 && newText.Split('.').Last().Length <= 2;
        }

        private void ValidateForm()
        {
            CreatePaymentButton.IsEnabled = OrderCombo.SelectedValue != null &&
                                           ReceiptCombo.SelectedValue != null &&
                                           !string.IsNullOrWhiteSpace(AmountTextBox.Text) &&
                                           decimal.TryParse(AmountTextBox.Text, out decimal amount) && amount > 0;
        }

        private void CreatePaymentButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (OrderCombo.SelectedValue is int orderNumber &&
                    ReceiptCombo.SelectedValue is int receiptNumber &&
                    decimal.TryParse(AmountTextBox.Text, out decimal amount))
                {
                    var payment = new Payments
                    {
                        OrderNumber = orderNumber,
                        ReceiptNumber = receiptNumber,
                        PaymentAmount = amount
                    };
                    _context.Payments.Add(payment);
                    _context.SaveChanges();
                    MessageBox.Show($"Платеж успешно создан! [{DateTime.Now:HH:mm:ss}]");
                    LoadData();
                }
                else
                {
                    MessageBox.Show("Выберите заказ, поступление и введите корректную сумму!");
                }
            }
            catch (Exception ex)
            {
                string errorMessage = ex.InnerException?.Message ?? ex.Message;
                string innerExceptionMessage = ex.InnerException?.InnerException?.Message ?? "Нет дополнительных деталей";
                MessageBox.Show($"Ошибка [{DateTime.Now:HH:mm:ss}]: {errorMessage}\nПодробности: {innerExceptionMessage}");
            }
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadData();
            MessageBox.Show($"Данные обновлены! [{DateTime.Now:HH:mm:ss}]");
        }
    }

    public class PaymentView
    {
        public int PaymentID { get; set; }
        public string OrderDescription { get; set; }
        public string ReceiptDescription { get; set; }
        public decimal PaymentAmount { get; set; }
    }
}