# Coffee Shop Console Application

Run from this project directory with `dotnet run`. JSON data and the application log are stored in the build output's `Data` directory, matching the WashingMachine application's local-data convention.

## Project structure

```text
CoffeeShopApp
├── Constants       Configuration, file paths, and common messages
├── Controllers     Application flow and controller interface
├── Enums           Coffee, order, machine, and menu states
├── Events          Order-ready notification event
├── Exceptions      Storage and business-operation errors
├── Helpers         Console coordination, logging, and password hashing
├── Models          Users, coffee, orders, machines, inventory, dashboard snapshot
├── Repository      Small JSON-backed data access operations
├── Services        Business rules, machine workers, and notification routing
├── Storage         JSON storage abstraction and implementation
└── Views           Validated console input, dashboard, customer, and admin views
```

## Application flow

- Sign up/login, then place coffee orders, review order history, cancel waiting orders, collect ready orders, and view spending.
- The menu includes Espresso, Cappuccino, Latte, Americano, and Mocha, with prices, recipes, stock, and preparation durations.
- Two asynchronous machine workers divide an order into individual servings, so both machines can prepare a large order at the same time. Espresso/Cappuccino/Latte preparation takes 5/10/15 seconds per serving after sourcing. The order becomes Ready when every serving finishes.
- Cancellation and the transition to Preparing are synchronized; only orders still in Placed status can be cancelled.
- Order-ready notifications use a delegate selected as console, email simulation, or SMS simulation.
- The fixed top dashboard refreshes machine availability, order counts, active orders, and completed sales while the console menus remain below it.
- Admin views include all orders, preparing/completed/today's orders, machine status, live preparation monitoring, and inventory restocking.
- Passwords are stored as salted PBKDF2 hashes. Interrupted preparing orders are returned to the queue when the app restarts.

The admin menu is a local demonstration view and is not protected by a separate administrator account.
