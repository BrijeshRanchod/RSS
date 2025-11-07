// ViewModels/LineItemViewModel.cs
using RSSPOS.Models;
using System.ComponentModel;
using System.Runtime.CompilerServices;
namespace RSSPOS.ViewModels;
public class LineItemViewModel : INotifyPropertyChanged
{
    private Service? _service;
    private int _quantity = 1;
    private decimal _unitPrice;

    public Service? Service
    {
        get => _service;
        set
        {
            if (_service == value) return;
            _service = value;
            // auto-pull price from selected service
            UnitPrice = _service?.Price ?? 0m;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ServiceId));
            OnPropertyChanged(nameof(LineTotal));
        }
    }

    public int ServiceId => Service?.Id ?? 0;

    public int Quantity
    {
        get => _quantity;
        set
        {
            if (value < 1) value = 1;
            if (_quantity == value) return;
            _quantity = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(LineTotal));
        }
    }

    public decimal UnitPrice
    {
        get => _unitPrice;
        private set
        {
            if (_unitPrice == value) return;
            _unitPrice = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(LineTotal));
        }
    }

    public decimal LineTotal => Quantity * UnitPrice;

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? n = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
}
