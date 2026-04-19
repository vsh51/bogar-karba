COMPOSE ?= docker compose

.PHONY: help up down logs clean-db reset-db

help:
	@echo "Targets:"
	@echo "  up        Start the stack in the background."
	@echo "  down      Stop the stack (volumes preserved)."
	@echo "  logs      Tail logs for all services."
	@echo "  clean-db  Stop stack and drop all named volumes."
	@echo "  reset-db  Clean volumes and start again (DB reseeds on boot)."

up:
	$(COMPOSE) up -d --build

down:
	$(COMPOSE) down

logs:
	$(COMPOSE) logs -f

clean-db:
	$(COMPOSE) down -v

reset-db: clean-db
	$(COMPOSE) up -d --build
