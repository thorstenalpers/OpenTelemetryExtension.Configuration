
helm uninstall sqlserver --wait --timeout 120s
helm install sqlserver ./chart-sqlserver --wait --timeout 120s

pause