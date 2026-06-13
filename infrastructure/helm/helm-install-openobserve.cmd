
helm uninstall openobserve --wait --timeout 120s
helm install openobserve ./chart-openobserve --wait --timeout 120s

pause