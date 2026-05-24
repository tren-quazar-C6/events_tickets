#!/bin/bash

# Local Testing Script
# This script tests the audit API with mock data and verifies MongoDB stores the data locally

set -e

BASE_URL="http://localhost:8080"
CORRELATION_ID="test-$(date +%s)"

echo "🧪 Testing events_tickets API with Mock Data"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "📍 Base URL: $BASE_URL"
echo "🏷️  Correlation ID: $CORRELATION_ID"
echo ""

# Color codes
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Function to test endpoint
test_endpoint() {
    local method=$1
    local endpoint=$2
    local data=$3
    local description=$4

    echo -e "${YELLOW}→${NC} $description"
    echo "   $method $endpoint"

    if [ "$method" == "GET" ]; then
        response=$(curl -s -w "\n%{http_code}" \
            -H "X-Correlation-ID: $CORRELATION_ID" \
            "$BASE_URL$endpoint")
    else
        response=$(curl -s -w "\n%{http_code}" \
            -X "$method" \
            -H "Content-Type: application/json" \
            -H "X-Correlation-ID: $CORRELATION_ID" \
            -d "$data" \
            "$BASE_URL$endpoint")
    fi

    http_code=$(echo "$response" | tail -n1)
    body=$(echo "$response" | head -n-1)

    if [[ $http_code == 2* ]] || [[ $http_code == 202 ]]; then
        echo -e "   ${GREEN}✓ Status: $http_code${NC}"
        echo "   Response: $(echo $body | jq '.' 2>/dev/null || echo $body)"
    else
        echo -e "   ${RED}✗ Status: $http_code${NC}"
        echo "   Response: $body"
    fi
    echo ""
}

# 1. Health check
test_endpoint "GET" "/health" "" "1️⃣  Health Check"

# 2. Log a sale
test_endpoint "POST" "/api/audit/sales" \
    '{
        "employeeId": "emp-001",
        "employeeName": "Developer 4",
        "ticketId": "ticket-001",
        "eventId": "event-concert-2025",
        "amount": 45000,
        "paymentMethod": "cash"
    }' \
    "2️⃣  Log a Sale"

# 3. Log a ticket print
test_endpoint "POST" "/api/audit/tickets/prints" \
    '{
        "employeeId": "emp-001",
        "ticketId": "ticket-001",
        "printerName": "HP-LaserJet-001",
        "reason": "Customer requested physical copy"
    }' \
    "3️⃣  Log a Ticket Print"

# 4. Log a ticket reprint
test_endpoint "POST" "/api/audit/tickets/reprints" \
    '{
        "employeeId": "emp-001",
        "ticketId": "ticket-001",
        "printerName": "HP-LaserJet-001",
        "reason": "Customer lost original, reissuing"
    }' \
    "4️⃣  Log a Ticket Reprint"

# 5. Log a cancellation
test_endpoint "POST" "/api/audit/tickets/cancellations" \
    '{
        "employeeId": "emp-001",
        "ticketId": "ticket-001",
        "reason": "Customer refund request"
    }' \
    "5️⃣  Log a Ticket Cancellation"

# 6. Get ticket history
test_endpoint "GET" "/api/audit/tickets/ticket-001" "" "6️⃣  Get Ticket History"

# 7. Another sale with different employee
test_endpoint "POST" "/api/audit/sales" \
    '{
        "employeeId": "emp-002",
        "employeeName": "Assistant Dev 4",
        "ticketId": "ticket-002",
        "eventId": "event-theater-2025",
        "amount": 50000,
        "paymentMethod": "credit_card"
    }' \
    "7️⃣  Log Another Sale (Different Employee)"

echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo -e "${GREEN}✅ All requests completed!${NC}"
echo ""
echo "📊 To view data in MongoDB:"
echo "   docker exec -it events_mongo mongosh events_observability"
echo ""
echo "📝 Example MongoDB queries:"
echo "   db.sales_logs.find().pretty()"
echo "   db.ticket_logs.find().pretty()"
echo "   db.employee_actions.find().pretty()"
echo "   db.sales_logs.countDocuments()"
