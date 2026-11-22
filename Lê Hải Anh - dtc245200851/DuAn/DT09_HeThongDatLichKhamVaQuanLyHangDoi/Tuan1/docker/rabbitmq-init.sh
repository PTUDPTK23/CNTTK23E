#!/bin/sh
set -eu
# Post-start hook: wait for broker, import topology, then ensure the configured user exists.
# Idempotent: preserve existing users/passwords and all persistent data.
for attempt in $(seq 1 60); do
    if rabbitmq-diagnostics -q ping >/dev/null 2>&1; then break; fi
    sleep 1
done
rabbitmqctl await_startup
rabbitmqctl import_definitions /etc/rabbitmq/definitions.json
if ! rabbitmqctl -q list_users | cut -f1 | grep -Fxq "$RABBITMQ_DEFAULT_USER"; then
    rabbitmqctl add_user "$RABBITMQ_DEFAULT_USER" "$RABBITMQ_DEFAULT_PASS"
    rabbitmqctl set_user_tags "$RABBITMQ_DEFAULT_USER" administrator
    rabbitmqctl set_permissions -p / "$RABBITMQ_DEFAULT_USER" '.*' '.*' '.*'
fi
touch /tmp/clinicqueue-topology.ready
