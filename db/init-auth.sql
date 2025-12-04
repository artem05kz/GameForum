CREATE TABLE IF NOT EXISTS auth_users (
  id INT AUTO_INCREMENT PRIMARY KEY,
  username VARCHAR(64) NOT NULL UNIQUE,
  password VARCHAR(255) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

INSERT INTO auth_users (username, password) VALUES
  ('admin', '$apr1$0L/GuQNe$gXuf6Iz7c4aqffnEd.5qk0вщ');
