#!/bin/bash

# MongoDB Query Script for VPS
# This script connects to MongoDB on your VPS via SSH and runs queries

set -e

# Configuration - CHANGE THESE
VPS_HOST="${1:-your-vps-hostname}"
VPS_USER="${2:-root}"
SSH_KEY="${3:-~/.ssh/id_rsa}"

echo "🔍 Connecting to MongoDB on VPS: $VPS_HOST"
echo "📍 Location: /opt/quasar/events_infrastructure"
echo ""

# Function to run MongoDB query via SSH
run_mongo_query() {
    local collection=$1
    local query=$2

    echo "═══════════════════════════════════════════════════════"
    echo "Collection: $collection"
    echo "═══════════════════════════════════════════════════════"

    ssh -i "$SSH_KEY" "$VPS_USER@$VPS_HOST" << EOF
        set -e
        cd /opt/quasar/events_infrastructure
        docker compose exec -T events_mongo mongosh events_observability << 'MONGO'
            $query
MONGO
EOF
    echo ""
}

# Query 1: Count all sales
run_mongo_query "sales_logs" "
db.sales_logs.countDocuments()
"

# Query 2: Show all sales
run_mongo_query "sales_logs" "
db.sales_logs.find().pretty()
"

# Query 3: Count all ticket logs
run_mongo_query "ticket_logs" "
db.ticket_logs.countDocuments()
"

# Query 4: Show all ticket logs
run_mongo_query "ticket_logs" "
db.ticket_logs.find().pretty()
"

# Query 5: Show all employee actions
run_mongo_query "employee_actions" "
db.employee_actions.find().pretty()
"

# Query 6: Show all system errors
run_mongo_query "system_errors" "
db.system_errors.find().pretty()
"

# Query 7: Get history of a specific ticket
read -p "📋 Enter ticket ID to search (or press Enter to skip): " TICKET_ID
if [ -n "$TICKET_ID" ]; then
    run_mongo_query "ticket_logs" "
    db.ticket_logs.find({ticketId: '$TICKET_ID'}).pretty()
    "
fi

echo "✅ Query complete!"
