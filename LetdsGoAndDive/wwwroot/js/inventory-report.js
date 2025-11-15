document.addEventListener('DOMContentLoaded', function () {
    const stockData = window.__inventoryInitial ? window.__inventoryInitial.stock : [];
    const salesData = window.__inventoryInitial ? window.__inventoryInitial.sales : [];

    // Pie - stock distribution
    const pieCtx = document.getElementById('pieStock').getContext('2d');

    // FIX: use ProductName (correct C# property)
    const pieLabels = stockData.map(s => s.ProductName);
    const pieValues = stockData.map(s => s.Quantity);

    new Chart(pieCtx, {
        type: 'pie',
        data: {
            labels: pieLabels,
            datasets: [{
                data: pieValues
            }]
        },
        options: { responsive: true }
    });

    // Bar - top selling products
    const barCtx = document.getElementById('barSales').getContext('2d');
    const topSales = salesData.slice(0, 10);

    // FIX: use ProductName and QuantitySold (correct C# properties)
    const barLabels = topSales.map(s => s.ProductName);
    const barValues = topSales.map(s => s.QuantitySold);

    new Chart(barCtx, {
        type: 'bar',
        data: {
            labels: barLabels,
            datasets: [{
                label: 'Quantity Sold',
                data: barValues
            }]
        },
        options: {
            responsive: true,
            scales: {
                y: { beginAtZero: true }
            }
        }
    });
});
