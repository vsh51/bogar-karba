# Вибрана архітектура

Було вирішено зупинитись на Clean Architecture: Layered, як у минулому семестрі, робити не хотілося, а Hexagon виглядав занадто "специфічним".

## Структура проекту

```
Domain           (Бізнес логіка)
Application      (Use-Case-и)
Infrastracture   (Інфраструктура)
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

### Infrastracture
```
Persistence/
Repositories/
Logging/
```

### Web
```
Models/
Views/
COntrollers/

wwwroot/
```
