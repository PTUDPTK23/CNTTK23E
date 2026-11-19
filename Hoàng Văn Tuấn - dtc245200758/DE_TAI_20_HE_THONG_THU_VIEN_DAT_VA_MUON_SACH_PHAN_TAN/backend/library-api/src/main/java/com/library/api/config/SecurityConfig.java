package com.library.api.config;

import com.library.api.service.JwtService;
import lombok.RequiredArgsConstructor;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import org.springframework.http.HttpMethod;
import org.springframework.security.config.Customizer;
import org.springframework.security.config.annotation.web.builders.HttpSecurity;
import org.springframework.security.config.http.SessionCreationPolicy;
import org.springframework.security.crypto.password.PasswordEncoder;
import org.springframework.security.web.SecurityFilterChain;
import org.springframework.security.web.authentication.UsernamePasswordAuthenticationFilter;

@Configuration
@RequiredArgsConstructor
public class SecurityConfig {

    private final JwtService jwtService;

    @Bean
    public SecurityFilterChain securityFilterChain(HttpSecurity http) throws Exception {
        JwtAuthenticationFilter jwtFilter = new JwtAuthenticationFilter(jwtService);

        http
            .csrf(csrf -> csrf.disable())
            .cors(Customizer.withDefaults())
            .sessionManagement(session ->
                    session.sessionCreationPolicy(SessionCreationPolicy.STATELESS))
            .formLogin(form -> form.disable())
            .httpBasic(basic -> basic.disable())
            .authorizeHttpRequests(auth -> auth

                .requestMatchers(HttpMethod.OPTIONS, "/**").permitAll()

                .requestMatchers(
                        "/api/auth/register",
                        "/api/auth/verify-email",
                        "/api/auth/resend-code",
                        "/api/auth/login"
                ).permitAll()

                .requestMatchers("/api/auth/me").authenticated()

                .requestMatchers(
                        "/api/health",
                        "/actuator/health",
                        "/actuator/info"
                ).permitAll()

                .requestMatchers(HttpMethod.GET,
                        "/api/books", "/api/books/**",
                        "/api/categories",
                        "/api/stats", "/api/stats/**",
                        "/api/system/pipeline"
                ).permitAll()

                .requestMatchers(HttpMethod.GET, "/api/users/*/reservations")
                    .authenticated()

                .requestMatchers(HttpMethod.POST,
                        "/api/reservations/*/borrow",
                        "/api/reservations/*/return"
                ).hasAnyRole("ADMIN", "LIBRARIAN")

                .requestMatchers(HttpMethod.GET, "/api/reservations")
                    .hasAnyRole("ADMIN", "LIBRARIAN")

                .requestMatchers("/api/reservations/**")
                    .authenticated()

                .requestMatchers("/api/users/**")
                    .hasAnyRole("ADMIN", "LIBRARIAN")

                .requestMatchers(HttpMethod.POST, "/api/reservations")
                    .authenticated()

                .requestMatchers(
                        "/api/books/**",
                        "/api/categories/**",
                        "/api/copies/**"
                ).hasAnyRole("ADMIN", "LIBRARIAN")

                .anyRequest().authenticated()
            )
            .addFilterBefore(jwtFilter, UsernamePasswordAuthenticationFilter.class);

        return http.build();
    }
}