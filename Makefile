COMPOSE ?= docker compose

IMAGE_REGISTRY ?= docker.io/vsh51
IMAGE_NAME     ?= bogar-karba
IMAGE_TAG      ?= latest
IMAGE          := $(IMAGE_REGISTRY)/$(IMAGE_NAME):$(IMAGE_TAG)

AZURE_RG       ?= student-app-rg
AZURE_APP      ?= myapp-web-8937
GIT_SHA        := $(shell git rev-parse --short HEAD 2>/dev/null)

.PHONY: help up down logs clean-db reset-db image-build image-push image-name redeploy

help:
	@echo "Targets:"
	@echo "  up           Start the stack in the background."
	@echo "  down         Stop the stack (volumes preserved)."
	@echo "  logs         Tail logs for all services."
	@echo "  clean-db     Stop stack and drop all named volumes."
	@echo "  reset-db     Clean volumes and start again (DB reseeds on boot)."
	@echo "  image-build  Build the app image as $(IMAGE)."
	@echo "  image-push   Push $(IMAGE) to the registry."
	@echo "  image-name   Print the full image reference."
	@echo "  redeploy     Build + push as git-SHA tag, repoint Azure Web App to it."

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

image-build:
	docker build -t $(IMAGE) .

image-push: image-build
	docker push $(IMAGE)

image-name:
	@echo $(IMAGE)

redeploy:
	@test -n "$(GIT_SHA)" || { echo "Not a git repo or no commits"; exit 1; }
	$(MAKE) image-push IMAGE_TAG=$(GIT_SHA)
	az webapp config container set \
	  -g $(AZURE_RG) -n $(AZURE_APP) \
	  --container-image-name $(IMAGE_REGISTRY)/$(IMAGE_NAME):$(GIT_SHA) -o none
	@echo "Deployed $(IMAGE_REGISTRY)/$(IMAGE_NAME):$(GIT_SHA) to $(AZURE_APP)"
