# Get the container ID of the running zoho-api-test container
container_id=$(sudo docker ps -q -f ancestor=amscreen-rdm-sim)

# Connect to the running container for debugging
if sudo docker exec -it $container_id /bin/bash; then
  echo "Connected using /bin/bash"
else
  sudo docker exec -it $container_id /bin/sh
fi