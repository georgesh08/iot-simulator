using Microsoft.AspNetCore.Builder;
using Prometheus;

namespace IoTController;

public class PrometheusServer
{
	public static void Start()
	{
		Task.Run(() =>
		{
			var builder = WebApplication.CreateBuilder();
			var app = builder.Build();

			app.UseHttpMetrics();
			DotNetStats.Register(Metrics.DefaultRegistry);

			app.MapMetrics("/metrics");

			app.Run("http://0.0.0.0:14620");
		});
	}
}
