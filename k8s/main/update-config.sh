kubectl apply -f deployment.yaml -n biatec-identity
kubectl delete configmap biatec-identity-helper-1-conf -n biatec-identity
kubectl create configmap biatec-identity-helper-1-conf --from-file=conf -n biatec-identity
kubectl rollout restart deployment/biatec-identity-helper-1-app-deployment -n biatec-identity
kubectl rollout status deployment/biatec-identity-helper-1-app-deployment -n biatec-identity
