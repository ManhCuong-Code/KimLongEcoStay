/**
 * Kim Long EcoStay - Financial & HR Chart.js Visualization Controller
 * Cung cấp biểu đồ trực quan hóa dữ liệu Doanh thu, Chi phí, Lợi nhuận và Cơ cấu nhân sự
 */

window.financialCharts = {
    instances: {},

    // Format tiền tệ VNĐ ngắn gọn (Ví dụ: 15.5 Tr đ hoặc 500k đ)
    formatCompactVND: function (value) {
        if (Math.abs(value) >= 1000000000) {
            return (value / 1000000000).toFixed(2) + ' Tỷ';
        }
        if (Math.abs(value) >= 1000000) {
            return (value / 1000000).toFixed(1) + ' Tr';
        }
        if (Math.abs(value) >= 1000) {
            return (value / 1000).toFixed(0) + ' k';
        }
        return value.toLocaleString('vi-VN') + ' đ';
    },

    // 1. Biểu đồ Phối Hợp: Doanh Thu, Chi Phí (Cột) & Lợi Nhuận (Đường) Theo Dòng Thời Gian
    renderTimelineChart: function (canvasId, labels, revenueData, expenseData, profitData) {
        var canvas = document.getElementById(canvasId);
        if (!canvas) return;

        if (this.instances[canvasId]) {
            this.instances[canvasId].destroy();
        }

        var ctx = canvas.getContext('2d');
        var self = this;

        this.instances[canvasId] = new Chart(ctx, {
            type: 'bar',
            data: {
                labels: labels,
                datasets: [
                    {
                        type: 'line',
                        label: 'Lợi Nhuận Ròng (VNĐ)',
                        data: profitData,
                        borderColor: '#B8860B',
                        backgroundColor: '#B8860B',
                        borderWidth: 3,
                        pointBackgroundColor: '#FFFFFF',
                        pointBorderColor: '#B8860B',
                        pointBorderWidth: 2,
                        pointRadius: 5,
                        pointHoverRadius: 7,
                        tension: 0.35,
                        yAxisID: 'y',
                        order: 1
                    },
                    {
                        type: 'bar',
                        label: 'Doanh Thu (VNĐ)',
                        data: revenueData,
                        backgroundColor: 'rgba(16, 185, 129, 0.85)',
                        hoverBackgroundColor: '#059669',
                        borderRadius: 6,
                        borderSkipped: false,
                        yAxisID: 'y',
                        order: 2
                    },
                    {
                        type: 'bar',
                        label: 'Chi Phí Vận Hành (VNĐ)',
                        data: expenseData,
                        backgroundColor: 'rgba(239, 68, 68, 0.85)',
                        hoverBackgroundColor: '#DC2626',
                        borderRadius: 6,
                        borderSkipped: false,
                        yAxisID: 'y',
                        order: 3
                    }
                ]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                interaction: {
                    mode: 'index',
                    intersect: false
                },
                plugins: {
                    legend: {
                        position: 'top',
                        labels: {
                            font: { family: "'Plus Jakarta Sans', sans-serif", weight: '600', size: 12 },
                            usePointStyle: true,
                            padding: 18
                        }
                    },
                    tooltip: {
                        backgroundColor: 'rgba(15, 23, 42, 0.92)',
                        titleFont: { family: "'Plus Jakarta Sans', sans-serif", size: 13, weight: '700' },
                        bodyFont: { family: "'Plus Jakarta Sans', sans-serif", size: 12 },
                        padding: 12,
                        cornerRadius: 8,
                        callbacks: {
                            label: function (context) {
                                var val = context.parsed.y || 0;
                                return context.dataset.label + ': ' + val.toLocaleString('vi-VN') + ' đ (' + self.formatCompactVND(val) + ')';
                            }
                        }
                    }
                },
                scales: {
                    x: {
                        grid: { display: false },
                        ticks: {
                            font: { family: "'Plus Jakarta Sans', sans-serif", weight: '500', size: 12 },
                            color: '#475569'
                        }
                    },
                    y: {
                        grid: { color: 'rgba(226, 232, 240, 0.8)' },
                        ticks: {
                            font: { family: "'Plus Jakarta Sans', sans-serif", size: 11 },
                            color: '#64748B',
                            callback: function (val) {
                                return self.formatCompactVND(val);
                            }
                        }
                    }
                }
            }
        });
    },

    // 2. Biểu đồ Donut Cơ cấu Chi Phí Theo Danh Mục
    renderExpenseDonutChart: function (canvasId, labels, data, colors) {
        var canvas = document.getElementById(canvasId);
        if (!canvas) return;

        if (this.instances[canvasId]) {
            this.instances[canvasId].destroy();
        }

        var ctx = canvas.getContext('2d');
        var self = this;
        var total = data.reduce(function (a, b) { return a + b; }, 0);

        this.instances[canvasId] = new Chart(ctx, {
            type: 'doughnut',
            data: {
                labels: labels,
                datasets: [{
                    data: data,
                    backgroundColor: colors,
                    borderWidth: 2,
                    borderColor: '#FFFFFF',
                    hoverOffset: 8
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                cutout: '68%',
                plugins: {
                    legend: {
                        position: 'bottom',
                        labels: {
                            font: { family: "'Plus Jakarta Sans', sans-serif", size: 11, weight: '500' },
                            padding: 12,
                            boxWidth: 12,
                            usePointStyle: true
                        }
                    },
                    tooltip: {
                        backgroundColor: 'rgba(15, 23, 42, 0.92)',
                        padding: 10,
                        cornerRadius: 8,
                        callbacks: {
                            label: function (context) {
                                var val = context.parsed || 0;
                                var pct = total > 0 ? ((val / total) * 100).toFixed(1) : '0';
                                return context.label + ': ' + val.toLocaleString('vi-VN') + ' đ (' + pct + '%)';
                            }
                        }
                    }
                }
            }
        });
    },

    // 3. Biểu đồ Donut Cơ cấu Doanh Thu Theo Nguồn Dịch Vụ
    renderRevenueDonutChart: function (canvasId, labels, data, colors) {
        var canvas = document.getElementById(canvasId);
        if (!canvas) return;

        if (this.instances[canvasId]) {
            this.instances[canvasId].destroy();
        }

        var ctx = canvas.getContext('2d');
        var self = this;
        var total = data.reduce(function (a, b) { return a + b; }, 0);

        this.instances[canvasId] = new Chart(ctx, {
            type: 'doughnut',
            data: {
                labels: labels,
                datasets: [{
                    data: data,
                    backgroundColor: colors,
                    borderWidth: 2,
                    borderColor: '#FFFFFF',
                    hoverOffset: 8
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                cutout: '68%',
                plugins: {
                    legend: {
                        position: 'bottom',
                        labels: {
                            font: { family: "'Plus Jakarta Sans', sans-serif", size: 11, weight: '500' },
                            padding: 12,
                            boxWidth: 12,
                            usePointStyle: true
                        }
                    },
                    tooltip: {
                        backgroundColor: 'rgba(15, 23, 42, 0.92)',
                        padding: 10,
                        cornerRadius: 8,
                        callbacks: {
                            label: function (context) {
                                var val = context.parsed || 0;
                                var pct = total > 0 ? ((val / total) * 100).toFixed(1) : '0';
                                return context.label + ': ' + val.toLocaleString('vi-VN') + ' đ (' + pct + '%)';
                            }
                        }
                    }
                }
            }
        });
    },

    // 4. Biểu đồ Cột Phân Bổ Nhân Sự Theo Phòng Ban
    renderDepartmentBarChart: function (canvasId, labels, data, colors) {
        var canvas = document.getElementById(canvasId);
        if (!canvas) return;

        if (this.instances[canvasId]) {
            this.instances[canvasId].destroy();
        }

        var ctx = canvas.getContext('2d');

        this.instances[canvasId] = new Chart(ctx, {
            type: 'bar',
            data: {
                labels: labels,
                datasets: [{
                    label: 'Số Lượng Nhân Sự',
                    data: data,
                    backgroundColor: colors,
                    borderRadius: 6,
                    borderSkipped: false
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: { display: false },
                    tooltip: {
                        callbacks: {
                            label: function (context) {
                                return context.parsed.y + ' Nhân sự';
                            }
                        }
                    }
                },
                scales: {
                    x: {
                        grid: { display: false },
                        ticks: { font: { family: "'Plus Jakarta Sans', sans-serif", size: 11 } }
                    },
                    y: {
                        beginAtZero: true,
                        ticks: {
                            stepSize: 1,
                            font: { family: "'Plus Jakarta Sans', sans-serif", size: 11 }
                        }
                    }
                }
            }
        });
    }
};
