#!/bin/sh
PASSWORD=$(cat /run/secrets/flyway_password)
flyway -password="$PASSWORD" "$@"