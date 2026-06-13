
helm repo add signoz https://charts.signoz.io
helm repo update

helm uninstall signoz --wait --timeout 300s
REM kubectl delete chi signoz-clickhouse --ignore-not-found --wait

helm install signoz signoz/signoz  --version 0.128.0  -f signoz.values.yaml --wait --timeout 600s

kubectl delete -f signoz.nodeports.yaml --ignore-not-found
kubectl apply -f signoz.nodeports.yaml

kubectl delete -f signoz.bootstrap-admin.yaml --ignore-not-found
kubectl apply -f signoz.bootstrap-admin.yaml

REM helm show values signoz/signoz  --version 0.128.0 > signoz.values.yaml

pause