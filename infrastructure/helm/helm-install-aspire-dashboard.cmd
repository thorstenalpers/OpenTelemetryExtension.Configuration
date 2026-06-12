
helm repo add aspire-dashboard https://kube-the-home.github.io/aspire-dashboard-helm/
helm repo update 

helm uninstall aspire-dashboard
helm install aspire-dashboard aspire-dashboard/aspire-dashboard --version 1.28.3  -f aspire-dashboard.values.yaml

kubectl delete -f aspire-dashboard.nodeports.yaml
kubectl apply -f aspire-dashboard.nodeports.yaml

REM helm show values aspire-dashboard/aspire-dashboard > values.aspire-dashboard.yaml

pause