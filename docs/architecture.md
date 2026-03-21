# Вибрана архітектура

Було вирішено зупинитись на Clean Architecture: Layered, як у минулому семестрі, робити не хотілося, а Hexagon виглядав занадто "специфічним".

## Структура проекту

```
Domain           (Бізнес логіка)
Application      (Use-Case-и)
Infrastructure   (Інфраструктура)
Web              (UI)
```

Детальніше:

### Domain
```
Entities/
ValueObjects/
Enums/
Exceptions/
...
```

### Application
```
UseCases/
Common/

Interfaces/

DTOs
Mappings/
Validation/
```

### Infrastructure
```
Persistence/
Repositories/
Identity/
```

### Web
```
Models/
Views/
Controllers/

wwwroot/
```
