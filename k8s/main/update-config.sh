kubectl apply -f deployment-1.yaml -n biatec-identity
kubectl delete configmap biatec-identity-helper-1-conf -n biatec-identity
kubectl create configmap biatec-identity-helper-1-conf --from-file=conf -n biatec-identity
kubectl rollout restart deployment/biatec-identity-helper-1-app-deployment -n biatec-identity

kubectl apply -f deployment-2.yaml -n biatec-identity
kubectl delete configmap biatec-identity-helper-2-conf -n biatec-identity
kubectl create configmap biatec-identity-helper-2-conf --from-file=conf -n biatec-identity
kubectl rollout restart deployment/biatec-identity-helper-2-app-deployment -n biatec-identity

kubectl rollout status deployment/biatec-identity-helper-1-app-deployment -n biatec-identity
kubectl rollout status deployment/biatec-identity-helper-2-app-deployment -n biatec-identity
