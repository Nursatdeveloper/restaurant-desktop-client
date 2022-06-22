using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace Restaurant_Order__Desktop_Interface
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        HubConnection connection;
        List<FoodOrder> foodOrders = new();
        ConnectionParameters parameters;
        FoodOrder acceptedOrder;
        public MainWindow()
        {
            InitializeComponent();
            connection = new HubConnectionBuilder()
                .WithUrl("https://localhost:6001/ws/orders")
                .WithAutomaticReconnect()
                .Build();

            
            appConnectionStatus.Text = "Disconnected";

            connection.Reconnecting += (sender) =>
            {
                this.Dispatcher.Invoke(() =>
                {
                    appConnectionStatus.Text = "Attempting to connect...";
                });

                return Task.CompletedTask;
            };

            connection.Reconnected += (sender) =>
            {
                this.Dispatcher.Invoke(async () =>
                {
                    appConnectionStatus.Text = "Connected...";

                    orders.Items.Clear();
                    foodOrders.Clear();
                    ConnectionParameters parameters = new ConnectionParameters()
                    {
                        RestaurantName = restaurant.Text,
                        Address = address.Text,
                        City = city.Text,
                        Password = password.Password
                    };
                    await connection.InvokeAsync("JoinOrderStreaming", parameters);
                });

                return Task.CompletedTask;
            };

            connection.Closed += (sender) =>
            {
                this.Dispatcher.Invoke(() =>
                {
                    appConnectionStatus.Text = "Disconnected...";
                });

                return Task.CompletedTask;
            };

            connection.On<string>("IncorrectPassword", (error) =>
            {
                this.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show(error);
                });
            });

            connection.On<List<FoodOrder>>("AvailableOrders", (availableOrders) =>
            {
                this.Dispatcher.Invoke(() =>
                {
                    foodOrders = availableOrders;
                    orders.ItemsSource = foodOrders;
                });
            });
            connection.On<FoodOrder>("NewOrder", (order) =>
            {
                this.Dispatcher.Invoke(() =>
                {
                    var newOrder = $"{order.RestaurantName}: {order.FoodName}";
                    foodOrders.Add(order);
                    orders.ItemsSource = foodOrders;
                    orders.Items.Refresh();
                });
            });
            connection.On<FoodOrder>("StatusUpdated", (updatedOrder) =>
            {
                this.Dispatcher.Invoke(() =>
                {
                    for (int i = 0; i < foodOrders.Count; i++)
                    {
                        if (foodOrders[i].Id == updatedOrder.Id)
                        {
                            foodOrders[i] = updatedOrder;
                            orders.ItemsSource = foodOrders;
                            orders.Items.Refresh();
                            break;
                        }
                    }
                });
            });

            connection.On<FoodOrder>("AcceptedByDelivery", (acceptedOrder) =>
            {
                this.Dispatcher.Invoke(() =>
                {
                    for (int i = 0; i < foodOrders.Count; i++)
                    {
                        if (foodOrders[i].Id == acceptedOrder.Id)
                        {
                            foodOrders[i] = acceptedOrder;
                            orders.ItemsSource = foodOrders;
                            orders.Items.Refresh();
                            break;
                        }
                    }
                });
            });

        }

        private async Task Connect()
        {
            try
            {
                parameters = new ConnectionParameters()
                {
                    RestaurantName = restaurant.Text,
                    Address = address.Text,
                    City = city.Text,
                    Password = password.Password
                };
                Console.WriteLine($"{restaurant.Text} {address.Text} {city.Text} {password.Password}");
                await connection.StartAsync();
                await connection.InvokeAsync("JoinOrderStreaming",
                parameters);
                appConnectionStatus.Text = "Connected";
            }
            catch (Exception ex)
            {
                orders.Items.Add(ex.Message);
            }
        }

        private async void authenticate_Click(object sender, RoutedEventArgs e)
        {
            await Connect();
        }

        private async void changeStatus_Click(object sender, RoutedEventArgs e)
        {
            Button _button = (Button)sender;
            var id = int.Parse(_button.Tag.ToString());
            var order = foodOrders.Where(o => o.Id == id).FirstOrDefault();
            if(order.Status == "Pending")
            {
                await connection.InvokeAsync("ChangeStatus", parameters, order.Id, "Executing");
            }
            else
            {
                await connection.InvokeAsync("ChangeStatus", parameters, order.Id, "Ready to deliver");
            }
        }

        private void orderDone_Click(object sender, RoutedEventArgs e)
        {
            Button _button = (Button)sender;
            var id = int.Parse(_button.Tag.ToString());
            var order = foodOrders.Where(o => o.Id == id).FirstOrDefault();
            InputBox.Visibility = System.Windows.Visibility.Visible;
            acceptedOrder = order;
        }

        private void SubmitButton_Click(object sender, RoutedEventArgs e)
        {
            InputBox.Visibility = System.Windows.Visibility.Collapsed;

            String input = InputTextBox.Text;
            if(acceptedOrder.DeliveryCode == input)
            {
                if (acceptedOrder.Status == "Ready to deliver")
                {
                    foodOrders.Remove(acceptedOrder);
                    orders.ItemsSource = foodOrders;
                    orders.Items.Refresh();
                }
                InputText.Text = "Delivery code is not correct";
            }

            InputTextBox.Text = String.Empty;
        }
    }
}
